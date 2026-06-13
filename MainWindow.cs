using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using Swed64;

namespace LumyxCheatsV2
{
    public class MainWindow : Window
    {
        public static Swed? SwedMemory;
        private FovWindow? fovWindow;
        private bool isRunning = true;

        private TextBlock txtStatus = new();
        private Button btnConnect = new();
        private ListBox espListBox = new ListBox();

        private bool isAimbotEnabled = false;
        private bool isNoRecoilEnabled = false;
        private bool isEspEnabled = false;

        private Grid dashboardPage = new();
        private Grid visualsPage = new();
        private Grid aimbotPage = new();

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        private const int MOUSEEVENTF_MOVE = 0x0001;

        // --- 💜 LOW-LEVEL NT SYSTEM CALLS (Omijanie OpenProcess) ---
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern uint NtOpenProcess(
            out IntPtr ProcessHandle,
            uint DesiredAccess,
            ref OBJECT_ATTRIBUTES ObjectAttributes,
            ref CLIENT_ID ClientId);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern uint NtReadVirtualMemory(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            byte[] Buffer,
            uint NumberOfBytesToRead,
            out uint NumberOfBytesRead);

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CLIENT_ID
        {
            public IntPtr UniqueProcess;
            public IntPtr UniqueThread;
        }

        // Używamy flagi 0x0010 (VM_READ) – absolutne systemowe minimum do odczytu bajtów
        private const uint NT_PROCESS_VM_READ = 0x0010;

        // --- BAZA SYSTEMU AUTOMATYCZNEJ HEURYSTYKI ---
        private static long DETECTED_UWORLD = 0;
        private static long DETECTED_GAME_STATE = 0x170;
        private static long DETECTED_PLAYER_ARRAY = 0x2B8;
        private static long DETECTED_PAWN_PRIVATE = 0x320;

        private const long OFFSET_GAME_INSTANCE = 0x1D8;
        private const long OFFSET_LOCAL_PLAYERS = 0x38;
        private const long OFFSET_PLAYER_CONTROLLER = 0x30;
        private const long OFFSET_ACKNOWLEDGED_PAWN = 0x338;
        private const long OFFSET_CURRENT_WEAPON = 0x948;
        private const long OFFSET_RECOIL_DATA = 0x1FE0;

        private Button StworzFioletowyPrzycisk(string tekst)
        {
            return new Button
            {
                Content = tekst,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 5),
                FontSize = 14,
                Height = 40
            };
        }
        private void PrzelaczStrone(Grid stronaDoPokazania)
        {
            dashboardPage.Visibility = Visibility.Collapsed;
            visualsPage.Visibility = Visibility.Collapsed;
            aimbotPage.Visibility = Visibility.Collapsed;
            stronaDoPokazania.Visibility = Visibility.Visible;
        }

        public MainWindow()
        {
            Title = "Lumyx Cheats V2";
            Width = 1200;
            Height = 700;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(11, 17, 32));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Grid mainGrid = new();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel sidebar = new() { Background = new SolidColorBrush(Color.FromRgb(17, 24, 39)), Margin = new Thickness(10) };

            TextBlock titleText = new() { Text = "LUMYX CHEATS", Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246)), FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(10, 20, 10, 5), HorizontalAlignment = HorizontalAlignment.Center };
            sidebar.Children.Add(titleText);

            txtStatus.Text = "Status: Oczekiwanie";
            txtStatus.Foreground = Brushes.Orange;
            txtStatus.HorizontalAlignment = HorizontalAlignment.Center;
            txtStatus.Margin = new Thickness(0, 0, 0, 20);
            sidebar.Children.Add(txtStatus);

            Button btnDashboard = StworzFioletowyPrzycisk("Dashboard");
            Button btnVisuals = StworzFioletowyPrzycisk("Visuals");
            Button btnAimbot = StworzFioletowyPrzycisk("Aimbot");
            Button btnExit = StworzFioletowyPrzycisk("Zamknij program");

            btnDashboard.Click += (s, e) => PrzelaczStrone(dashboardPage);
            btnVisuals.Click += (s, e) => PrzelaczStrone(visualsPage);
            btnAimbot.Click += (s, e) => PrzelaczStrone(aimbotPage);
            btnExit.Click += (s, e) => { isRunning = false; Close(); };

            sidebar.Children.Add(btnDashboard);
            sidebar.Children.Add(btnVisuals);
            sidebar.Children.Add(btnAimbot);
            sidebar.Children.Add(btnExit);
            Grid.SetColumn(sidebar, 0);
            mainGrid.Children.Add(sidebar);
            Grid pagesContainer = new() { Margin = new Thickness(10) };

            // Dashboard Page
            StackPanel dashPanel = new() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock dashTitle = new() { Text = "Dashboard", Foreground = Brushes.White, FontSize = 28, Margin = new Thickness(0, 0, 0, 20), HorizontalAlignment = HorizontalAlignment.Center };
            btnConnect = StworzFioletowyPrzycisk("Niskopoziomowy Skan NT");
            btnConnect.Width = 250;
            btnConnect.Height = 50;
            btnConnect.Click += Button_Polacz_Click;
            dashPanel.Children.Add(dashTitle);
            dashPanel.Children.Add(btnConnect);
            dashboardPage.Children.Add(dashPanel);

            // Visuals Page
            StackPanel visualsPanel = new() { VerticalAlignment = System.Windows.VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(20) };
            TextBlock visualsTitle = new() { Text = "Visuals (Player ESP Radar)", Foreground = Brushes.White, FontSize = 28, Margin = new Thickness(0, 0, 0, 10) };
            CheckBox chkBoxEsp = new() { Content = "Enable Active ESP Overlay", Foreground = Brushes.White, FontSize = 16, Margin = new Thickness(0, 5, 0, 15) };

            chkBoxEsp.Checked += (s, e) => { isEspEnabled = true; };
            chkBoxEsp.Unchecked += (s, e) => { isEspEnabled = false; espListBox.Items.Clear(); };

            espListBox.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            espListBox.Foreground = Brushes.LimeGreen;
            espListBox.Height = 400;
            espListBox.FontSize = 14;

            visualsPanel.Children.Add(visualsTitle);
            visualsPanel.Children.Add(chkBoxEsp);
            visualsPanel.Children.Add(espListBox);
            visualsPage.Children.Add(visualsPanel);
            visualsPage.Visibility = Visibility.Collapsed;

            // Aimbot Page
            StackPanel aimbotPanel = new() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock aimbotTitle = new() { Text = "Aimbot & Weapon Mods", Foreground = Brushes.White, FontSize = 28, Margin = new Thickness(0, 0, 0, 20) };
            CheckBox chkAimbot = new() { Content = "Enable legit Aimbot", Foreground = Brushes.White, FontSize = 16, Margin = new Thickness(0, 10, 0, 10) };
            CheckBox chkFov = new() { Content = "Enable FOV Circle Overlay", Foreground = Brushes.White, FontSize = 16, Margin = new Thickness(0, 10, 0, 10) };
            CheckBox chkNoRecoil = new() { Content = "Enable No Recoil (Memory)", Foreground = Brushes.White, FontSize = 16, Margin = new Thickness(0, 10, 0, 10) };

            chkAimbot.Checked += (s, e) => { isAimbotEnabled = true; };
            chkAimbot.Unchecked += (s, e) => { isAimbotEnabled = false; };
            chkFov.Checked += (s, e) => { if (fovWindow == null) fovWindow = new FovWindow(); fovWindow.Show(); };
            chkFov.Unchecked += (s, e) => { fovWindow?.Hide(); };
            chkNoRecoil.Checked += NoRecoilToggle_Checked;
            chkNoRecoil.Unchecked += (s, e) => { isNoRecoilEnabled = false; };

            aimbotPanel.Children.Add(aimbotTitle);
            aimbotPanel.Children.Add(chkAimbot);
            aimbotPanel.Children.Add(chkFov);
            aimbotPanel.Children.Add(chkNoRecoil);
            aimbotPage.Children.Add(aimbotPanel);
            aimbotPage.Visibility = Visibility.Collapsed;

            pagesContainer.Children.Add(dashboardPage);
            pagesContainer.Children.Add(visualsPage);
            pagesContainer.Children.Add(aimbotPage);

            Grid.SetColumn(pagesContainer, 1);
            mainGrid.Children.Add(pagesContainer);
            Content = mainGrid;

            Task.Run(() => CheatLoop());
        }

        private void Button_Polacz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process[] procesy = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
                if (procesy.Length > 0)
                {
                    Process targetProcess = procesy[0];
                    SwedMemory = new Swed("FortniteClient-Win64-Shipping");
                    IntPtr baseAddress = targetProcess.MainModule.BaseAddress;

                    // Przygotowanie struktur dla jądra NT
                    OBJECT_ATTRIBUTES objAttr = new OBJECT_ATTRIBUTES();
                    objAttr.Length = Marshal.SizeOf(typeof(OBJECT_ATTRIBUTES));
                    CLIENT_ID clientId = new CLIENT_ID();
                    clientId.UniqueProcess = (IntPtr)targetProcess.Id;

                    IntPtr pHandle = IntPtr.Zero;

                    // 💜 WYWOŁANIE NT_API: Otwieramy proces bezpośrednio przez podsystem NT jądra Windows
                    uint status = NtOpenProcess(out pHandle, NT_PROCESS_VM_READ, ref objAttr, ref clientId);

                    if (status == 0 && pHandle != IntPtr.Zero)
                    {
                        // Bezpiecznie skanujemy obszar pamięci RAM za pomocą niskopoziomowego odczytu
                        byte[] buffer = new byte[0x10000]; // Skanujemy blokami po 64KB
                        IntPtr currentAddr = baseAddress;

                        for (int k = 0; k < 1000; k++) // Przeszukujemy pamięć wokół modułu bazowego
                        {
                            uint bytesRead;
                            if (NtReadVirtualMemory(pHandle, currentAddr, buffer, (uint)buffer.Length, out bytesRead) == 0)
                            {
                                for (int i = 0; i < buffer.Length - 8; i += 8)
                                {
                                    long potencjalnyUWorld = BitConverter.ToInt64(buffer, i);
                                    if (potencjalnyUWorld > 0 && potencjalnyUWorld % 8 == 0)
                                    {
                                        long testGameInstance = (long)SwedMemory.ReadLong((IntPtr)(potencjalnyUWorld + OFFSET_GAME_INSTANCE));
                                        if (testGameInstance > 0 && testGameInstance % 8 == 0)
                                        {
                                            DETECTED_UWORLD = (long)currentAddr + i;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (DETECTED_UWORLD > 0) break;
                            currentAddr = (IntPtr)((long)currentAddr + buffer.Length);
                        }
                    }

                    if (DETECTED_UWORLD > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtStatus.Text = "Status: NT_OK!";
                            txtStatus.Foreground = Brushes.LimeGreen;
                            btnConnect.Content = "Silnik NT Aktywny";
                            btnConnect.IsEnabled = false;
                        });

                        MessageBox.Show("Sukces! Niskopoziomowy silnik NT pomyślnie zsynchronizował struktury pamięci!", "Połączono", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Jeśli heurystyka nie zdążyła zweryfikować struktur, używamy oficjalnego offsetu Sterna jako bazy awaryjnej
                        DETECTED_UWORLD = (long)baseAddress + 0x145DAB80;

                        Dispatcher.Invoke(() =>
                        {
                            txtStatus.Text = "Status: NT_FALLBACK";
                            txtStatus.Foreground = Brushes.LimeGreen;
                            btnConnect.Content = "Połączenie Awaryjne NT";
                            btnConnect.IsEnabled = false;
                        });
                    }
                }
                else
                {
                    MessageBox.Show("Nie znaleziono procesu gry. Uruchom serwer i Fortnite!", "Oczekiwanie", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd niskopoziomowego podsystemu NT: {ex.Message}");
            }
        }

        private void NoRecoilToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (SwedMemory == null)
            {
                MessageBox.Show("Najpierw aktywuj Skanowanie na Dashboardzie!", "Brak inicjalizacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (sender is CheckBox chk) chk.IsChecked = false;
                return;
            }
            isNoRecoilEnabled = true;
        }
        private void CheatLoop()
        {
            bool wydanoDzwiekTestowy = false;
            int licznikEsp = 0;

            while (isRunning)
            {
                if (SwedMemory != null && DETECTED_UWORLD > 0)
                {
                    try
                    {
                        long uWorld = (long)SwedMemory.ReadLong((IntPtr)DETECTED_UWORLD);

                        if (uWorld > 0)
                        {
                            if (!wydanoDzwiekTestowy)
                            {
                                Console.Beep(1200, 400);
                                wydanoDzwiekTestowy = true;
                            }

                            long gameInstance = (long)SwedMemory.ReadLong((IntPtr)(uWorld + OFFSET_GAME_INSTANCE));
                            long localPlayers = (long)SwedMemory.ReadLong((IntPtr)(gameInstance + OFFSET_LOCAL_PLAYERS));
                            long localPlayer = (long)SwedMemory.ReadLong((IntPtr)localPlayers);
                            long playerController = (long)SwedMemory.ReadLong((IntPtr)(localPlayer + OFFSET_PLAYER_CONTROLLER));
                            long localPawn = (long)SwedMemory.ReadLong((IntPtr)(playerController + OFFSET_ACKNOWLEDGED_PAWN));

                            if (isNoRecoilEnabled && localPawn > 0)
                            {
                                long currentWeapon = (long)SwedMemory.ReadLong((IntPtr)(localPawn + OFFSET_CURRENT_WEAPON));
                                if (currentWeapon > 0)
                                {
                                    long recoilData = currentWeapon + OFFSET_RECOIL_DATA;
                                    SwedMemory.WriteFloat((IntPtr)(recoilData + 0x0), 0.0f);
                                    SwedMemory.WriteFloat((IntPtr)(recoilData + 0x4), 0.0f);
                                }
                            }

                            if (isAimbotEnabled || isEspEnabled)
                            {
                                long gameState = (long)SwedMemory.ReadLong((IntPtr)(uWorld + DETECTED_GAME_STATE));
                                long playerArray = (long)SwedMemory.ReadLong((IntPtr)(gameState + DETECTED_PLAYER_ARRAY));
                                int playerCount = SwedMemory.ReadInt((IntPtr)(gameState + (DETECTED_PLAYER_ARRAY + 8)));

                                licznikEsp++;
                                bool odswiezajEsp = isEspEnabled && (licznikEsp % 10 == 0);

                                if (odswiezajEsp)
                                {
                                    Dispatcher.Invoke(() => espListBox.Items.Clear());
                                }

                                for (int i = 0; i < playerCount; i++)
                                {
                                    long playerState = (long)SwedMemory.ReadLong((IntPtr)(playerArray + (i * 8)));
                                    long pawnPrivate = (long)SwedMemory.ReadLong((IntPtr)(playerState + DETECTED_PAWN_PRIVATE));

                                    if (pawnPrivate > 0 && pawnPrivate != localPawn)
                                    {
                                        long rootComponent = (long)SwedMemory.ReadLong((IntPtr)(pawnPrivate + 0x1A8));
                                        float enemyX = SwedMemory.ReadFloat((IntPtr)(rootComponent + 0x128));

                                        if (odswiezajEsp)
                                        {
                                            int dystans = (int)(Math.Abs(enemyX) / 100);
                                            Dispatcher.Invoke(() => {
                                                espListBox.Items.Add($"[BOT/GRACZ] ID: {i} | Dystans: {dystans}m");
                                            });
                                        }

                                        if (isAimbotEnabled)
                                        {
                                            int ruchX = 5;
                                            int ruchY = 2;

                                            mouse_event(MOUSEEVENTF_MOVE, ruchX, ruchY, 0, 0);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
