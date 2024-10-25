using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WheelGame
{
    public partial class MainWindow : Window
    {
        private int selectedPrizeAmount = 0;  // Player's selected prize amount
        private bool isSpinning = false;

        // Prize amounts corresponding to each of the 20 segments
        private readonly int[] prizeAmounts = new int[]
        {
            5, 10, 20, 50, 100, 200, 500, 1000, 5000, 10000,
            20000, 50000, 75000, 100000, 125000, 150000, 175000, 200000, 225000, 250000
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        // Handle the prize amount selection
        private void PrizeAmountComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PrizeAmountComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Content?.ToString(), out int prizeAmount))
                {
                    selectedPrizeAmount = prizeAmount;
                }
                else
                {
                    selectedPrizeAmount = 0;
                    MessageBox.Show("Invalid prize amount selected.");
                }
            }
        }

        // Spin button click handler
        private async void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSpinning) return;  // Prevent multiple clicks during spin
            isSpinning = true;

            SpinButton.IsEnabled = false;

            var tickPlayer = new MediaPlayer();
            string tickSoundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Tick.wav");

            if (!System.IO.File.Exists(tickSoundPath))
            {
                MessageBox.Show($"Tick.wav not found at: {tickSoundPath}");
                SpinButton.IsEnabled = true;
                isSpinning = false;
                return;
            }

            tickPlayer.Open(new Uri(tickSoundPath, UriKind.Absolute));

            // Check if the player has selected a prize amount
            if (selectedPrizeAmount == 0)
            {
                MessageBox.Show("Please select a win amount before spinning.");
                SpinButton.IsEnabled = true;
                isSpinning = false;
                return;
            }

            // Find the index of the selected prize amount
            int prizeIndex = Array.IndexOf(prizeAmounts, selectedPrizeAmount);
            if (prizeIndex == -1)
            {
                MessageBox.Show("Selected prize amount is invalid.");
                SpinButton.IsEnabled = true;
                isSpinning = false;
                return;
            }

            // Calculate the stop angle based on the selected prize
            double segmentSize = 18.0;  // 20 segments of 18 degrees each
            double stopAngle = prizeIndex * segmentSize;

            await SpinWheelWithTicks(stopAngle, tickPlayer);  // Spin the wheel

            // Display the prize won
            PrizeDisplay.Text = $"You won R{selectedPrizeAmount}!";

            SpinButton.IsEnabled = true;
            isSpinning = false;
        }

        // Spins the wheel and synchronizes tick sounds with segment crossings
        private async Task SpinWheelWithTicks(double stopAngle, MediaPlayer tickPlayer)
        {
            double fullRotations = 5;  // 5 full rotations
            double totalRotation = 360 * fullRotations + stopAngle;  // Total rotation angle
            double segmentSize = 18.0;  // 20 segments, each 18 degrees
            int lastSegment = -1;  // Track the last ticked segment

            DoubleAnimation spinAnimation = new DoubleAnimation
            {
                From = 0,
                To = totalRotation,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            // Start the animation
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);

            // Subscribe to the CompositionTarget.Rendering event
            EventHandler renderingHandler = null;
            renderingHandler = (s, e) =>
            {
                // Get the current angle from the RotateTransform
                double currentAngle = (WheelRotation.Angle % 360 + 360) % 360;  // Normalize angle between 0-360

                int currentSegment = (int)(currentAngle / segmentSize);  // Calculate segment

                if (currentSegment != lastSegment)  // Play tick if new segment is crossed
                {
                    tickPlayer.Stop();
                    tickPlayer.Play();
                    lastSegment = currentSegment;
                }
            };

            CompositionTarget.Rendering += renderingHandler;

            // Wait for the animation to complete
            await Task.Delay(spinAnimation.Duration.TimeSpan);

            // Unsubscribe from the event
            CompositionTarget.Rendering -= renderingHandler;
        }
    }
}
