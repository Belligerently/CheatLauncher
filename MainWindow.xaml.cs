using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ModernLauncher
{
    public partial class MainWindow : Window
    {
        private string ExecutablesFolder = "";
        private List<string> DetectedExecutables = new List<string>();
        private List<string> FilteredExecutables = new List<string>();
        private HashSet<string> FavoriteApps = new HashSet<string>();
        private string FavoritesFilePath = "";
        private bool IsShowingDashboard = true;
        private FileSystemWatcher? folderWatcher;

        public MainWindow()
        {
            InitializeComponent();
            
            SetupExecutablesFolder();
            LoadFavorites();
            RefreshExecutables();
            SetupSearchPlaceholder();
            SetupFolderWatcher();
            UpdateSidebarSelection(); // Set initial sidebar selection
            this.Loaded += (s, e) => this.Focus(); // Enable keyboard shortcuts
        }

        private void SetupExecutablesFolder()
        {
            // Get the directory where the executable is located
            // Use AppContext.BaseDirectory for single-file apps, fallback to Assembly.Location for framework-dependent
            string? appDirectory = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(appDirectory))
            {
                appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            
            ExecutablesFolder = Path.Combine(appDirectory ?? Environment.CurrentDirectory, "Apps");
            FavoritesFilePath = Path.Combine(appDirectory ?? Environment.CurrentDirectory, "favorites.txt");
            
            if (!Directory.Exists(ExecutablesFolder))
            {
                Directory.CreateDirectory(ExecutablesFolder);
            }
        }

        private void LoadFavorites()
        {
            FavoriteApps.Clear();
            if (File.Exists(FavoritesFilePath))
            {
                var favorites = File.ReadAllLines(FavoritesFilePath);
                foreach (var favorite in favorites)
                {
                    if (!string.IsNullOrWhiteSpace(favorite))
                    {
                        FavoriteApps.Add(favorite.Trim());
                    }
                }
            }
        }

        private void SaveFavorites()
        {
            File.WriteAllLines(FavoritesFilePath, FavoriteApps);
        }

        private void ToggleFavorite(string exePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(exePath);
            if (FavoriteApps.Contains(fileName))
            {
                FavoriteApps.Remove(fileName);
            }
            else
            {
                FavoriteApps.Add(fileName);
            }
            SaveFavorites();
            UpdateLaunchOptions();
        }

        private SolidColorBrush GetBrushFromHex(string hexColor)
        {
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
        }

        private void RefreshExecutables()
        {
            DetectedExecutables = new List<string>();
            
            if (Directory.Exists(ExecutablesFolder))
            {
                var exeFiles = Directory.GetFiles(ExecutablesFolder, "*.exe");
                DetectedExecutables.AddRange(exeFiles);
            }
            
            UpdateLaunchOptions();
        }

        private void UpdateLaunchOptions()
        {
            // Clear existing options (except the title)
            var children = LaunchOptionsPanel.Children.Cast<UIElement>().ToList();
            for (int i = children.Count - 1; i >= 1; i--)
            {
                LaunchOptionsPanel.Children.RemoveAt(i);
            }

            // Update app count and title based on current view
            var baseExecutables = FilteredExecutables?.Count > 0 ? FilteredExecutables : DetectedExecutables;
            var executablesToShow = IsShowingDashboard 
                ? baseExecutables.Where(exe => FavoriteApps.Contains(Path.GetFileNameWithoutExtension(exe))).ToList()
                : baseExecutables;

            if (LaunchOptionsPanel.FindName("AppCountText") is TextBlock appCountText)
            {
                appCountText.Text = IsShowingDashboard 
                    ? $"{executablesToShow.Count} Favorites" 
                    : $"{executablesToShow.Count} Apps";
            }

            // Update title text
            if (LaunchOptionsPanel.Children.Count > 0 && LaunchOptionsPanel.Children[0] is TextBlock titleText)
            {
                titleText.Text = IsShowingDashboard ? "Favorite Applications" : "Available Applications";
            }

            if (executablesToShow.Count == 0)
            {
                // Show appropriate empty state
                var emptyStatePanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 60, 0, 0)
                };

                var emptyIcon = new TextBlock
                {
                    Text = IsShowingDashboard ? "⭐" : "📦",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var emptyTitle = new TextBlock
                {
                    Text = IsShowingDashboard ? "No Favorite Applications" : "No Applications Found",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var emptyDescription = new TextBlock
                {
                    Text = IsShowingDashboard 
                        ? "Mark applications as favorites by clicking the star icon in the Applications tab."
                        : "Click 'Open Folder' to add executable files to get started.",
                    FontSize = 14,
                    Foreground = GetBrushFromHex("#999999"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 24),
                    TextAlignment = TextAlignment.Center
                };

                emptyStatePanel.Children.Add(emptyIcon);
                emptyStatePanel.Children.Add(emptyTitle);
                emptyStatePanel.Children.Add(emptyDescription);

                if (!IsShowingDashboard)
                {
                    var openFolderBtn = new Button
                    {
                        Content = "📂 Open Apps Folder",
                        Style = (Style)FindResource("ModernButtonStyle"),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    openFolderBtn.Click += (s, e) => OpenFolder_Click(s, e);
                    emptyStatePanel.Children.Add(openFolderBtn);
                }

                LaunchOptionsPanel.Children.Add(emptyStatePanel);
                return;
            }

            // Create grid for app cards
            var appsGrid = new Grid();
            appsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            appsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i < executablesToShow.Count; i++)
            {
                if (i % 2 == 0)
                {
                    appsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                var appCard = CreateModernAppCard(executablesToShow[i]);
                Grid.SetColumn(appCard, i % 2);
                Grid.SetRow(appCard, i / 2);
                appCard.Margin = new Thickness(i % 2 == 0 ? 0 : 12, 0, i % 2 == 0 ? 12 : 0, 16);

                appsGrid.Children.Add(appCard);
            }

            LaunchOptionsPanel.Children.Add(appsGrid);
        }

        private Border CreateModernAppCard(string exePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(exePath);
            var fileInfo = new FileInfo(exePath);
            var fileSize = FormatFileSize(fileInfo.Length);
            var lastModified = fileInfo.LastWriteTime.ToString("MMM dd, yyyy");

            // Main card container
            var card = new Border
            {
                Background = GetBrushFromHex("#1A1A1A"),
                CornerRadius = new CornerRadius(12),
                BorderBrush = GetBrushFromHex("#2A2A2A"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.3,
                    BlurRadius = 10,
                    ShadowDepth = 2
                }
            };

            // Card content
            var cardContent = new StackPanel();

            // Header with icon and title
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new Border
            {
                Background = GetBrushFromHex("#4F46E5"),
                CornerRadius = new CornerRadius(8),
                Width = 40,
                Height = 40
            };

            // Try to extract and use the actual app icon
            try
            {
                var iconSource = ExtractIcon(exePath);
                if (iconSource != null)
                {
                    icon.Child = new System.Windows.Controls.Image
                    {
                        Source = iconSource,
                        Width = 32,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                else
                {
                    // Fallback to emoji icon
                    icon.Child = new TextBlock
                    {
                        Text = "🎮",
                        FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
            catch
            {
                // Fallback to emoji icon on any error
                icon.Child = new TextBlock
                {
                    Text = "🎮",
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            var titleStack = new StackPanel
            {
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = fileName,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var statusBadge = new Border
            {
                Background = GetBrushFromHex("#10B981"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = "Ready",
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Medium
                }
            };

            // Favorite button
            var favoriteBtn = new Button
            {
                Content = FavoriteApps.Contains(fileName) ? "⭐" : "☆",
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = FavoriteApps.Contains(fileName) ? GetBrushFromHex("#FFC107") : GetBrushFromHex("#999999"),
                BorderThickness = new Thickness(0),
                FontSize = 16,
                Width = 32,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Top,
                ToolTip = FavoriteApps.Contains(fileName) ? "Remove from favorites" : "Add to favorites"
            };
            favoriteBtn.Click += (s, e) => ToggleFavorite(exePath);

            titleStack.Children.Add(titleText);
            titleStack.Children.Add(statusBadge);

            Grid.SetColumn(icon, 0);
            Grid.SetColumn(titleStack, 1);
            Grid.SetColumn(favoriteBtn, 2);
            header.Children.Add(icon);
            header.Children.Add(titleStack);
            header.Children.Add(favoriteBtn);

            // Info section
            var infoGrid = new Grid
            {
                Margin = new Thickness(0, 16, 0, 0)
            };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition());
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var sizeInfo = new StackPanel();
            var sizeLabel = new TextBlock
            {
                Text = "Size",
                FontSize = 11,
                Foreground = GetBrushFromHex("#999999"),
                FontWeight = FontWeights.Medium
            };
            var sizeValue = new TextBlock
            {
                Text = fileSize,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 2, 0, 0)
            };
            sizeInfo.Children.Add(sizeLabel);
            sizeInfo.Children.Add(sizeValue);

            var dateInfo = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var dateLabel = new TextBlock
            {
                Text = "Modified",
                FontSize = 11,
                Foreground = GetBrushFromHex("#999999"),
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var dateValue = new TextBlock
            {
                Text = lastModified,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 2, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            dateInfo.Children.Add(dateLabel);
            dateInfo.Children.Add(dateValue);

            Grid.SetColumn(sizeInfo, 0);
            Grid.SetColumn(dateInfo, 1);
            infoGrid.Children.Add(sizeInfo);
            infoGrid.Children.Add(dateInfo);

            // Launch button
            var launchButton = new Button
            {
                Content = "🚀 Launch Application",
                Style = (Style)FindResource("ModernButtonStyle"),
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = exePath
            };
            launchButton.Click += (sender, e) => LaunchExecutable(exePath);

            // Assemble card
            cardContent.Children.Add(header);
            cardContent.Children.Add(infoGrid);
            cardContent.Children.Add(launchButton);

            card.Child = cardContent;
            return card;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void LaunchExecutable(string exePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(exePath);
                
                // Show custom themed popup
                var popup = ShowCustomPopup($"Launching {fileName}...", "🚀");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                };
                
                var process = Process.Start(processInfo);
                
                if (process != null)
                {
                    // Wait a moment then check if process is running
                    await Task.Delay(2000);
                    
                    // Try to find the process by name
                    var processes = Process.GetProcessesByName(fileName);
                    if (processes.Length > 0)
                    {
                        // Process is running, close popup
                        popup.Close();
                        
                        // Wait for process to exit, then show completion message
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                processes[0].WaitForExit();
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    ShowCustomPopup($"{fileName} has closed", "✅", 2000);
                                });
                            }
                            catch { }
                        });
                    }
                    else
                    {
                        // Process might have closed quickly or launched differently
                        await Task.Delay(1000);
                        popup.Close();
                    }
                }
                else
                {
                    popup.Close();
                    ShowCustomPopup($"Failed to launch {fileName}", "❌", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowCustomPopup($"Error: {ex.Message}", "❌", 5000);
            }
        }

        private Window ShowCustomPopup(string message, string icon, int autoCloseMs = 0)
        {
            var popup = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Width = 350,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ShowInTaskbar = false,
                Topmost = true
            };

            var border = new Border
            {
                Background = GetBrushFromHex("#161616"),
                CornerRadius = new CornerRadius(12),
                BorderBrush = GetBrushFromHex("#4F46E5"),
                BorderThickness = new Thickness(2),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.5,
                    BlurRadius = 15,
                    ShadowDepth = 3
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 15, 0)
            };

            var messageText = new TextBlock
            {
                Text = message,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 20, 0)
            };

            Grid.SetColumn(iconText, 0);
            Grid.SetColumn(messageText, 1);
            grid.Children.Add(iconText);
            grid.Children.Add(messageText);

            border.Child = grid;
            popup.Content = border;

            // Add fade in animation
            popup.Opacity = 0;
            popup.Show();
            
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            popup.BeginAnimation(OpacityProperty, fadeIn);

            // Auto close if specified
            if (autoCloseMs > 0)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(autoCloseMs)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (s2, e2) => popup.Close();
                    popup.BeginAnimation(OpacityProperty, fadeOut);
                };
                timer.Start();
            }

            return popup;
        }

        private void ShowCustomStatusPopup(string message)
        {
            try
            {
                var popup = new Window
                {
                    WindowStyle = WindowStyle.None,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = 350,
                    Height = 220,
                    Topmost = true,
                    ShowInTaskbar = false
                };

                var border = new Border
                {
                    Background = GetBrushFromHex("#161616"),
                    CornerRadius = new CornerRadius(16),
                    BorderBrush = GetBrushFromHex("#4F46E5"),
                    BorderThickness = new Thickness(2),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.5,
                        BlurRadius = 15,
                        ShadowDepth = 3
                    }
                };

                var mainStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(25),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var titleLabel = new Label
                {
                    Content = "System Status",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var messageTextBlock = new TextBlock
                {
                    Text = message,
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20,
                    Margin = new Thickness(0, 0, 0, 15),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };

                var closeButton = new Button
                {
                    Content = "Close",
                    Width = 100,
                    Height = 35,
                    Background = GetBrushFromHex("#4F46E5"),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Cursor = Cursors.Hand
                };

                // Apply the same modern button style as in XAML
                var buttonStyle = new Style(typeof(Button));
                buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, GetBrushFromHex("#4F46E5")));
                buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, System.Windows.Media.Brushes.White));
                buttonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0, 0, 0, 0)));
                buttonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(16, 8, 16, 8)));
                buttonStyle.Setters.Add(new Setter(Button.FontWeightProperty, FontWeights.SemiBold));
                
                var buttonTemplate = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                
                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                
                borderFactory.AppendChild(contentPresenterFactory);
                buttonTemplate.VisualTree = borderFactory;
                
                // Add triggers for hover and press effects
                var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, GetBrushFromHex("#6366F1")));
                buttonTemplate.Triggers.Add(hoverTrigger);
                
                var pressTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                pressTrigger.Setters.Add(new Setter(Button.BackgroundProperty, GetBrushFromHex("#3730A3")));
                buttonTemplate.Triggers.Add(pressTrigger);
                
                buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
                closeButton.Style = buttonStyle;

                closeButton.Click += (s, e) => popup.Close();

                mainStack.Children.Add(titleLabel);
                mainStack.Children.Add(messageTextBlock);
                mainStack.Children.Add(closeButton);
                border.Child = mainStack;
                popup.Content = border;

                popup.Opacity = 0;
                popup.Show();

                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                popup.BeginAnimation(OpacityProperty, fadeIn);
            }
            catch (Exception)
            {
                // Fallback to simple message box if popup fails
                System.Windows.MessageBox.Show($"Status: {message}", "Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Sidebar button events
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            IsShowingDashboard = true;
            UpdateLaunchOptions();
            UpdateSidebarSelection();
        }

        private void Applications_Click(object sender, RoutedEventArgs e)
        {
            IsShowingDashboard = false;
            UpdateLaunchOptions();
            UpdateSidebarSelection();
        }

        private void UpdateSidebarSelection()
        {
            // Reset all button backgrounds
            DashboardBtn.Background = System.Windows.Media.Brushes.Transparent;
            ApplicationsBtn.Background = System.Windows.Media.Brushes.Transparent;
            
            // Highlight the active button
            if (IsShowingDashboard)
            {
                DashboardBtn.Background = GetBrushFromHex("#2A2A2A");
            }
            else
            {
                ApplicationsBtn.Background = GetBrushFromHex("#2A2A2A");
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ExecutablesFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshExecutables();
            ShowCustomPopup("Applications refreshed successfully!", "🔄", 2000);
        }

        private void Status_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string statusMessage = $"Applications Found: {DetectedExecutables.Count}\n";
                statusMessage += $"Favorites: {FavoriteApps.Count}\n";
                statusMessage += $"View: {(IsShowingDashboard ? "Dashboard" : "Applications")}";
                
                ShowCustomStatusPopup(statusMessage);
            }
            catch (Exception ex)
            {
                ShowCustomPopup($"Error displaying status: {ex.Message}", "Error", 5000);
            }
        }

        // Search functionality
        private void SetupSearchPlaceholder()
        {
            SearchBox.GotFocus += SearchBox_GotFocus;
            SearchBox.LostFocus += SearchBox_LostFocus;
            SearchBox.Text = "Search applications...";
            SearchBox.Foreground = GetBrushFromHex("#666666");
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Search applications...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Search applications...";
                SearchBox.Foreground = GetBrushFromHex("#666666");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text == "Search applications...") return;
            
            FilterApplications(SearchBox.Text);
        }

        private void FilterApplications(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredExecutables = DetectedExecutables.ToList();
            }
            else
            {
                FilteredExecutables = DetectedExecutables
                    .Where(exe => Path.GetFileNameWithoutExtension(exe)
                        .ToLower().Contains(searchText.ToLower()))
                    .ToList();
            }
            UpdateLaunchOptions();
        }

        // Keyboard shortcuts
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                RefreshExecutables();
                e.Handled = true;
            }
            else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (SearchBox.IsFocused)
                {
                    SearchBox.Text = "";
                    FilterApplications("");
                    this.Focus();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.F1)
            {
                ShowKeyboardShortcuts();
                e.Handled = true;
            }
        }

        private void ShowKeyboardShortcuts()
        {
            string shortcuts = "Keyboard Shortcuts:\n\n" +
                             "F5 - Refresh applications\n" +
                             "Ctrl+F - Focus search box\n" +
                             "Escape - Clear search / Close\n" +
                             "F1 - Show this help\n" +
                             "Enter - Launch selected app";
            ShowCustomStatusPopup(shortcuts);
        }

        // App icon extraction
        private BitmapSource? ExtractIcon(string filePath)
        {
            try
            {
                using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath))
                {
                    if (icon != null)
                    {
                        return Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch
            {
                // Return default icon if extraction fails
            }
            return null;
        }

        // Folder auto-detection
        private void SetupFolderWatcher()
        {
            try
            {
                folderWatcher = new FileSystemWatcher(ExecutablesFolder)
                {
                    Filter = "*.exe",
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
                };

                folderWatcher.Created += OnNewExecutableAdded;
                folderWatcher.Deleted += OnExecutableRemoved;
                folderWatcher.Renamed += OnExecutableRenamed;
            }
            catch (Exception ex)
            {
                // Silently handle watcher setup failure
                Debug.WriteLine($"Failed to setup folder watcher: {ex.Message}");
            }
        }

        private void OnNewExecutableAdded(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshExecutables();
                ShowCustomPopup($"New application detected: {Path.GetFileNameWithoutExtension(e.Name)}", "🎉", 3000);
            });
        }

        private void OnExecutableRemoved(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshExecutables();
                ShowCustomPopup($"Application removed: {Path.GetFileNameWithoutExtension(e.Name)}", "🗑️", 2000);
            });
        }

        private void OnExecutableRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshExecutables();
                ShowCustomPopup($"Application renamed: {Path.GetFileNameWithoutExtension(e.Name)}", "✏️", 2000);
            });
        }

        // System tray integration
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
