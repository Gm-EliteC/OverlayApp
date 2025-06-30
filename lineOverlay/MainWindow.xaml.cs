using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Input;
using lineOverlay;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System;
using System.Drawing;



namespace VerticalLineOverlay
{



    public partial class MainWindow : Window
    {



        private System.Windows.Point _startPoint;
        private bool _isDragging = false;
        private System.Windows.Point _originalPosition;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

 

        // Import necessary Windows API functions
        
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const int SW_RESTORE = 9;

  
        public MainWindow()
        {
            InitializeComponent();
      
            StartNamedPipeListener();

            
            var (screenWidth, screenHeight) = GetGameResolution();

            // Position the window in the center of the screen
            this.Left = (screenWidth - this.Width)/2;
            this.Top = (screenHeight - this.Height)/2;

            DraggableContainer.MouseLeftButtonDown += DraggableContainer_MouseLeftButtonDown;
            DraggableContainer.MouseMove += DraggableContainer_MouseMove;
            DraggableContainer.MouseLeftButtonUp += DraggableContainer_MouseLeftButtonUp;

            ListenForShutdownSignal();

        }

        
        
        private void DraggableContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the original position
            _originalPosition = new System.Windows.Point(DraggableContainer.Margin.Left, DraggableContainer.Margin.Top);

            // Capture the starting point relative to the container
            _startPoint = e.GetPosition(DraggableContainer);
            _isDragging = true;
            DraggableContainer.CaptureMouse();
        }


        private void DraggableContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                // Get the current mouse position relative to the window
                System.Windows.Point currentPoint = e.GetPosition(this);

                // Calculate the offset from the original starting point
                double offsetX = currentPoint.X - (_startPoint.X + _originalPosition.X);
                double offsetY = currentPoint.Y - (_startPoint.Y + _originalPosition.Y);

                // Update the margin
                DraggableContainer.Margin = new Thickness(
                    _originalPosition.X + offsetX,
                    _originalPosition.Y + offsetY,
                    this.ActualWidth - (DraggableContainer.ActualWidth + _originalPosition.X + offsetX),
                    this.ActualHeight - (DraggableContainer.ActualHeight + _originalPosition.Y + offsetY)
                );
            }
        }

        private void DraggableContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            DraggableContainer.ReleaseMouseCapture();
        }



        private void StartNamedPipeListener()
        {

        
            Task.Run(() => {
                while (true)
                {
                    
                    try
                    {
                        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("AIMPipe", PipeDirection.In, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                        {
                            Debug.WriteLine("Waiting for connection...");
                            pipeServer.WaitForConnection();
                            Debug.WriteLine("Connected!");

                            using (StreamReader reader = new StreamReader(pipeServer))
                            {
                                string? message;
                                int counter = 0;
                                while ((message = reader.ReadLine()) != null)
                                {

                                    if (message == "reset")
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            VerticalLineLayer.Visibility = Visibility.Collapsed;
                                            ClubType.Visibility = Visibility.Collapsed;
                                            Caliper.Visibility = Visibility.Collapsed;
                                            PS.Visibility = Visibility.Collapsed;
                                            ShotType.Visibility = Visibility.Collapsed;
                                        });

                                        Debug.WriteLine("Reset triggered. All relevant UI elements have been hidden.");
                                        break; // Exit the method early
                                    }

                                        if (counter == 0)
                                    {
                                        Dispatcher.Invoke(() => ProcessAIMValue(message));


                                    }
                                    if (counter == 1)
                                    {

                                        string[] SpandCu = message.Split(',');

                                        Dispatcher.Invoke(() =>
                                        {
                                            SpinInputBox.Text = SpandCu[0];
                                            CurveInput.Text = SpandCu[1];
                                            int x = int.Parse(SpandCu[0]);
                                            int y = int.Parse(SpandCu[1]);
                                            movemouse(x, y);
                                        });

                                    }
                                    if (counter == 2)
                                    {

                                        string[] CaliperText = message.Split(',');


                                        Dispatcher.Invoke(() =>
                                        {
                                            Caliper.Text = CaliperText[0]; // Update the text
                                            Caliper.Visibility = Visibility.Visible; // Make it visible
                                        });

                                       
                                       
                                    }
                                    if (counter == 3)
                                    {
                                        
                                        Dispatcher.Invoke(() =>
                                        {
                                            Debug.WriteLine($"message is :{message}");
                                            ProcessShotType(message);
                                        });
                                        
                                    }
                                    if (counter == 4)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            Debug.WriteLine($"message is :{message}");
                                            ProcesPS(message);
                                        });
                                        
                                    }

                                    if (counter == 5)
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                           ProcessClubType(message);
                                        });
                                        break;
                                    }

                                    

                                    counter += 1;
                                }
                            }

                            pipeServer?.Dispose();
                        }
                    
                
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Pipe error: {ex.Message}");
                        // Add a small delay to prevent tight error loop
                        Thread.Sleep(1000);
                    }

                }
            });

            
        }


        private async void ListenForShutdownSignal()
        {
            await Task.Run(() =>
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("LineOverlayPipe", PipeDirection.In))
                {
                    pipeServer.WaitForConnection();
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string message = reader.ReadLine();
                        if (message == "shutdown")
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
                        }
                    }
                }
            });
        }


        private void ProcessClubType(string clubIndex)
        {

            var clubMapping = new string[]
            {
                "1W", "2W", "3W", "2I", "3I", "4I", "5I", "6I", "7I", "8I", "9I", "PW", "SW"
            };

            if (int.TryParse(clubIndex, out int index) && index >= 0 && index < clubMapping.Length)
            {
                string club = clubMapping[index]; // Map index to club
                ClubType.Text = club; // Display the club in a TextBox (or any UI element)
            }
            else
            {
                Debug.WriteLine($"Invalid club index received: {clubIndex}");
                ClubType.Text = "Invalid Club"; // Handle invalid index gracefully
            }

            
            
            ClubType.Visibility = Visibility.Visible; // Make the text visible
        }

        private void ProcesPS(string PSType)
        {


            // Update the text
            if (PSType == "0")
            {
                PS.Text = "No PS";
            }
            if(PSType == "1")
            {
                PS.Text = "1 PS";
            }
            if (PSType == "2")
            {
                PS.Text = "2PS";
            }
            if(PSType == "3")
            {
                PS.Text = "1.5 PS";
            }

            PS.Visibility = Visibility.Visible; // Make it visible

        }

        private void ProcessShotType(string shotType)
        {
            
            if (shotType == "0")
            {
                shotType = "Dunk";
            }
            
            if (shotType == "1")
            {
                shotType = "Tomahawk";
            }
            
            if (shotType == "2")
            {
                shotType = "Spike";
            }

            if (shotType == "3")
            {
                shotType = "Cobra";
            }
           

            // Update the text
            ShotType.Text = shotType;
            
            // Make the text visible
            ShotType.Visibility = Visibility.Visible;
        }

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private void ProcessAIMValue(string aimValue)
        {

            

            if (double.TryParse(aimValue, out double value))
            {
                // Get the game resolution
                var (gameWidth, gameHeight) = GetGameResolution();
                if (gameWidth == 0 || gameHeight == 0)
                {
                    Debug.WriteLine("Game resolution detection failed.");
                    return;
                }

                // Get the game window's rectangle
                IntPtr hwnd = FindWindow(null, "Pangya Reborn!"); // Adjust window title as needed
                if (hwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("Game window detection failed.");
                    return;
                }

                // Create a POINT structure for the client area center
                POINT clientCenter = new POINT
                {
                    X = (gameWidth / 2) +7,
                    Y = gameHeight / 2
                };

                if (!ClientToScreen(hwnd, ref clientCenter))
                {
                    Debug.WriteLine("Failed to convert client coordinates to screen coordinates.");
                    return;
                }

                // Determine the pixel factor based on game resolution
                int pixFactor = GetPixelFactor(gameWidth);

                // Movement based on AIM value
                double movement = value * pixFactor;

                // Calculate the new horizontal position using screen coordinates
                double newPositionX = clientCenter.X - movement;

                // Update the vertical line's position
                VerticalLineLayer.X1 = newPositionX;
                VerticalLineLayer.X2 = newPositionX;
                VerticalLineLayer.Y1 = clientCenter.Y - 34; // Half of 1 inch (96 pixels)
                VerticalLineLayer.Y2 = clientCenter.Y + 85; // Half of 1 inch

                VerticalLineLayer.Visibility = Visibility.Visible;

                // Debug logs for validation
                Debug.WriteLine($"AIM Value: {aimValue}, Movement: {movement}, GameCenterX: {clientCenter.X}, PixFactor: {pixFactor}, NewPositionX: {newPositionX}");
            }
            else
            {
                VerticalLineLayer.Visibility = Visibility.Collapsed;
                
            }
        }


        private void AimTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(AimTextBox.Text, out double value))
            {
                ProcessAIMValue(value.ToString());
                VerticalLineLayer.Visibility = Visibility.Visible; // Show the line if valid input
            }
            else
            {
                VerticalLineLayer.Visibility = Visibility.Collapsed; // Hide the line if invalid input
            }
        }


        private static int GetPixelFactor(int gameWidth)
        {
            return gameWidth switch
            {
                2560 => 108,
                2544 => 108,
                3840 => 162,
                2048 => 86,
                1920 => 81,
                1904 => 81,
                1600 => 68,
                1280 => 72,
                _ => 72 // Default to a reasonable value
            };
        }

        

        public static (int Width, int Height) CurrentResolution
        {
            get
            {
                // Use SystemParameters to get the primary screen resolution
                int width = (int)SystemParameters.PrimaryScreenWidth;
                int height = (int)SystemParameters.PrimaryScreenHeight;

                Debug.WriteLine($"CurrentResolution: {width}x{height}");
                return (width, height);
            }
        }



        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);


        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);


        //uSed for AIM
        private static (int Width, int Height) GetGameResolution()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resolution_log.txt");

            void LogMessage(string message)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}";
                Debug.WriteLine(logMessage);
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }

            // Log screen information first
            LogMessage($"Primary Screen Resolution: {SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}");
            LogMessage($"Virtual Screen: {SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}");

            // Log information about all screens
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                LogMessage($"Screen: {screen.DeviceName}");
                LogMessage($"  Bounds: {screen.Bounds.Width}x{screen.Bounds.Height} at ({screen.Bounds.X},{screen.Bounds.Y})");
                LogMessage($"  Working Area: {screen.WorkingArea.Width}x{screen.WorkingArea.Height}");
                LogMessage($"  Primary: {screen.Primary}");
                LogMessage($"  Scale Factor: {screen.Bounds.Width / SystemParameters.PrimaryScreenWidth:F2}x");
            }

            // Find the game window
            IntPtr hwnd = FindWindow(null, "Pangya Reborn!");
            if (hwnd == IntPtr.Zero)
            {
                LogMessage("Game window not found.");
                return (0, 0);
            }

            // Verify window is visible and not minimized
            if (!IsWindowVisible(hwnd))
            {
                LogMessage("Game window is not visible.");
                return (0, 0);
            }

            if (IsIconic(hwnd))
            {
                LogMessage("Game window is minimized.");
                return (0, 0);
            }

            // Get both client and window rectangles for validation
            if (!GetClientRect(hwnd, out RECT clientRect) || !GetWindowRect(hwnd, out RECT windowRect))
            {
                int error = Marshal.GetLastWin32Error();
                LogMessage($"Failed to get game window dimensions. Error: {error}");
                return (0, 0);
            }

            // Calculate dimensions
            int clientWidth = clientRect.Right - clientRect.Left;
            int clientHeight = clientRect.Bottom - clientRect.Top;
            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            // Validate reasonable dimensions
            if (clientWidth <= 0 || clientHeight <= 0 || windowWidth <= 0 || windowHeight <= 0)
            {
                LogMessage($"Invalid window dimensions detected: Client: {clientWidth}x{clientHeight}, Window: {windowWidth}x{windowHeight}");
                return (0, 0);
            }

            // Validate that client area is smaller than window (due to borders)
            if (clientWidth >= windowWidth || clientHeight >= windowHeight)
            {
                LogMessage("Warning: Client area should be smaller than window area. Possible window style issues.");
                LogMessage($"Client: {clientWidth}x{clientHeight}, Window: {windowWidth}x{windowHeight}");
            }

            // Log window position information
            LogMessage($"Window Position - Left: {windowRect.Left}, Top: {windowRect.Top}, Right: {windowRect.Right}, Bottom: {windowRect.Bottom}");

            // Log detailed information for debugging
            LogMessage($"Window dimensions - Full: {windowWidth}x{windowHeight}, Client: {clientWidth}x{clientHeight}");
            LogMessage($"Border sizes - Horizontal: {windowWidth - clientWidth}, Vertical: {windowHeight - clientHeight}");
            LogMessage($"Game resolution detected: {clientWidth}x{clientHeight}");

            return (clientWidth, clientHeight);
        }

        //Used for Spin
        private static (int Width, int Height)? GetGameResolution2()
        {
            IntPtr hwnd = FindWindow(null, "Pangya Reborn!");
            if (hwnd == IntPtr.Zero)
            {
                Debug.WriteLine("Game window not found.");
                return (0, 0);
            }

            if (GetWindowRect(hwnd, out RECT rect))
            {
                int width = (rect.Right - rect.Left); //16 for the border
                int height = (rect.Bottom - rect.Top); //39 for the border
                //Debug.WriteLine($"Game resolution detected: {width-16}x{height-39}");
                return (width, height);
            }
            else
            {
                Debug.WriteLine("Failed to get game window dimensions.");
                return (0, 0);
            }
        }




        // Define the dictionary to store the base positions of Spin and curve at different resolutions
        private static double GetPixelSpinFactor(int gameWidth)
        {
            return gameWidth switch
            {
                3840 => 4.5,
                2560 => 3,
                2544 => 3,
                2048 => 2.37,
                1920 => 2.280,
                1904 => 2.280,
                1600 => 1.868,
                1280 => 2,
                _ => 2 // Default to a reasonable value
            };
        }

        private static (double x, double y) GetBasePixelFactor(int gameWidth)
        {
            return gameWidth switch
            {
                3856 => (749, 1930), //3840 resolution + border
                2576 => (502, 1297),//2560 resolution + border
                2560 => (494, 1266), //resolution no border 
                2064 => (403, 1045), //2048 resolution + border
                1936 => (379, 981),//1920 resolution + border
                1920 => (370, 950), //resolution no border
                1616 => (317, 823),//1600 resolution + border
                1296 => (124, 875), //1280 resolution + border
                _ => ShowResolutionNotSupported() // Default to a reasonable value
            };
        }

        private static bool DontShowResolutionWarning
        {
            get => lineOverlay.Properties.Settings.Default.DontShowResolutionWarning;
            set
            {
                lineOverlay.Properties.Settings.Default.DontShowResolutionWarning = value;
                lineOverlay.Properties.Settings.Default.Save();
            }
        }


        private static (double x, double y) ShowResolutionNotSupported()
        {
            if (!DontShowResolutionWarning)
            {
                var result = System.Windows.MessageBox.Show(
                    "Resolution not Supported, use Spin 0, or 30!\n" +
                    "Use Wide Screen, 2560x1440 or 1280x960 in Window Mode\n" +
                    "Primary Screen Scaling 100%\n\n" +
                    "Would you like to hide this message in the future?",
                    "Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DontShowResolutionWarning = true;
                }
            }
            return (200, 200); // Default to a reasonable value
        }
        public double GetFactors()
        {
            // Get the game resolution
            var (gameWidth, gameHeight) = GetGameResolution();
            if (gameWidth == 0 || gameHeight == 0)
            {
                Debug.WriteLine("Game resolution detection failed.");
                return 0;
            }
            // Determine the pixel factor based on game resolution
            double spinFactor = GetPixelSpinFactor(gameWidth);
            //Debug.WriteLine($"Spin Factor: {spinFactor}, Curve Factor: {spinFactor}");
            return spinFactor;
        }


        private void SetSpinZero_Click(object sender, RoutedEventArgs e)
        {
            // Find the window with title "Pangya Reborn"
            IntPtr hwnd = FindWindow(null, "Pangya Reborn!");

            if (hwnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("Pangya Reborn window not found.");
                return;
            }


            // Get the game resolution
            var gameResolution = GetGameResolution2();
            if (!gameResolution.HasValue)
            {
                System.Windows.MessageBox.Show("Failed to detect game resolution.");
                return;
            }

            int gameWidth = gameResolution.Value.Width;

            // Get base pixel factor for the resolution
            var basePixelFactor = GetBasePixelFactor(gameWidth);
            if (basePixelFactor == default)
            {
                System.Windows.MessageBox.Show("Resolution not supported.");
                return;
            }

            // Get the window's position and size
            if (GetWindowRect(hwnd, out RECT rect))
            {
                // Calculate target position using the base pixel factor
                int targetX = rect.Left + (int)basePixelFactor.x;
                int targetY = rect.Top + (int)basePixelFactor.y;

                // Move the cursor to the calculated position
                SetCursorPos(targetX, targetY);

                // Simulate a left mouse click
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)targetX, (uint)targetY, 0, 0);
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to get window position.");
            }
            
            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            // Bring the window to the foreground
            SetForegroundWindow(hwnd);

        }

        private void SpinAndCurveOkButton(object sender, RoutedEventArgs e)
        {

         
            // Find the window with title "Pangya Reborn"
            IntPtr hwnd = FindWindow(null, "Pangya Reborn!");

            if (hwnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("Pangya Reborn window not found.");
                return;
            }


            // Get the window's rectangle to ensure we're using the correct window position
            if (!GetWindowRect(hwnd, out RECT rect))
            {
                System.Windows.MessageBox.Show("Failed to get window position.");
                return;
            }

            // Get the screen containing the game
            Screen gameScreen = Screen.FromHandle(hwnd);
            int screenLeft = gameScreen.Bounds.Left; // The left boundary of the screen
            int screenTop = gameScreen.Bounds.Top;   // The top boundary of the screen


            double gameWidth, gameHeight;
            var dimentions = GetGameResolution2();
            gameWidth = dimentions.Value.Width;
            gameHeight = dimentions.Value.Height;


            // Calculate monitor center (relative to the game screen)
            double monitorCenterX = screenLeft + (gameScreen.Bounds.Width / 2.0);
            double monitorCenterY = screenTop + (gameScreen.Bounds.Height / 2.0);

            // Calculate game center (relative to game resolution)
            double gameCenterX = rect.Left + (gameWidth / 2.0);
            double gameCenterY = rect.Top + (gameHeight / 2.0);

            // Get base offsets for game resolution
            (double spinX_0, double y_0) = GetBasePixelFactor((int)gameWidth);

            // Calculate offsets relative to the game center
            double offsetX = spinX_0 - (gameWidth / 2.0); // Relative to the center of the game
            double offsetY = y_0 - (gameHeight / 2.0);    // Relative to the center of the game



            // Default to 0 if input is empty
            string spinInput = string.IsNullOrWhiteSpace(SpinInputBox.Text) ? "0" : SpinInputBox.Text;
            string curveInput = string.IsNullOrWhiteSpace(CurveInput.Text) ? "0" : CurveInput.Text;

            if (double.TryParse(spinInput, out double spinValue) && double.TryParse(curveInput, out double curveValue))
            {
                //Get Scaling factos for Spin and Curve adjustments
                double spinFactor = GetFactors();
                double curveFactor = spinFactor;

                // Apply user input to the offsets
                double dynamicOffsetY = spinValue * spinFactor;
                double dynamicOffsetX = curveValue * curveFactor;

                // Calculate the final target position
                double targetX = gameCenterX + offsetX + dynamicOffsetX;
                double targetY = gameCenterY + offsetY + dynamicOffsetY;

                // Debugging outputs
                Debug.WriteLine($"Monitor Bounds: Left {screenLeft}, Top {screenTop}");
                Debug.WriteLine($"Monitor Center: X {monitorCenterX}, Y {monitorCenterY}");
                Debug.WriteLine($"Game Center (global): X {gameCenterX}, Y {gameCenterY}");
                Debug.WriteLine($"Base Offsets: X {offsetX}, Y {offsetY}");
                Debug.WriteLine($"Spin Input: {spinValue}, Curve Input: {curveValue}");
                Debug.WriteLine($"Dynamic Offsets: X {dynamicOffsetX}, Y {dynamicOffsetY}");
                Debug.WriteLine($"Final Target Position: X {targetX}, Y {targetY}");


                // Move the cursor to the calculated position
                SetCursorPos((int)targetX, (int)targetY);
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid input.");
            }


            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            // Bring the window to the foreground
            SetForegroundWindow(hwnd);

            Console.SetOut(new System.IO.StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private void movemouse(int x, int y)
        {
            // Find the window with title "Pangya Reborn"
            IntPtr hwnd = FindWindow(null, "Pangya Reborn!");

            if (hwnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("Pangya Reborn window not found.");
                return;
            }


            // Get the window's rectangle to ensure we're using the correct window position
            if (!GetWindowRect(hwnd, out RECT rect))
            {
                System.Windows.MessageBox.Show("Failed to get window position.");
                return;
            }

            // Get the screen containing the game
            Screen gameScreen = Screen.FromHandle(hwnd);
            int screenLeft = gameScreen.Bounds.Left; // The left boundary of the screen
            int screenTop = gameScreen.Bounds.Top;   // The top boundary of the screen


            double gameWidth, gameHeight;
            var dimentions = GetGameResolution2();
            gameWidth = dimentions.Value.Width;
            gameHeight = dimentions.Value.Height;


            // Calculate monitor center (relative to the game screen)
            double monitorCenterX = screenLeft + (gameScreen.Bounds.Width / 2.0);
            double monitorCenterY = screenTop + (gameScreen.Bounds.Height / 2.0);

            // Calculate game center (relative to game resolution)
            double gameCenterX = rect.Left + (gameWidth / 2.0);
            double gameCenterY = rect.Top + (gameHeight / 2.0);

            // Get base offsets for game resolution
            (double spinX_0, double y_0) = GetBasePixelFactor((int)gameWidth);

            // Calculate offsets relative to the game center
            double offsetX = spinX_0 - (gameWidth / 2.0); // Relative to the center of the game
            double offsetY = y_0 - (gameHeight / 2.0);    // Relative to the center of the game



            // Default to 0 if input is empty
            string spinInput = string.IsNullOrWhiteSpace(SpinInputBox.Text) ? "0" : SpinInputBox.Text;
            string curveInput = string.IsNullOrWhiteSpace(CurveInput.Text) ? "0" : CurveInput.Text;

            if (double.TryParse(spinInput, out double spinValue) && double.TryParse(curveInput, out double curveValue))
            {
                //Get Scaling factos for Spin and Curve adjustments
                double spinFactor = GetFactors();
                double curveFactor = spinFactor;

                // Apply user input to the offsets
                double dynamicOffsetY = spinValue * spinFactor;
                double dynamicOffsetX = curveValue * curveFactor;

                // Calculate the final target position
                double targetX = gameCenterX + offsetX + dynamicOffsetX;
                double targetY = gameCenterY + offsetY + dynamicOffsetY;

                // Debugging outputs
                Debug.WriteLine($"Monitor Bounds: Left {screenLeft}, Top {screenTop}");
                Debug.WriteLine($"Monitor Center: X {monitorCenterX}, Y {monitorCenterY}");
                Debug.WriteLine($"Game Center (global): X {gameCenterX}, Y {gameCenterY}");
                Debug.WriteLine($"Base Offsets: X {offsetX}, Y {offsetY}");
                Debug.WriteLine($"Spin Input: {spinValue}, Curve Input: {curveValue}");
                Debug.WriteLine($"Dynamic Offsets: X {dynamicOffsetX}, Y {dynamicOffsetY}");
                Debug.WriteLine($"Final Target Position: X {targetX}, Y {targetY}");


                // Move the cursor to the calculated position
                SetCursorPos((int)targetX, (int)targetY);
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid input.");
            }


            // Bring the window to the foreground
            SetForegroundWindow(hwnd);

            Console.SetOut(new System.IO.StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        }

    }

}