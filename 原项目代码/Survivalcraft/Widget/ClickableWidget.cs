namespace Game {
    public class ClickableWidget : Widget {
        public string SoundName { get; set; }

        public bool IsPressed { get; set; }

        public bool IsClicked { get; set; }

        public bool IsTapped { get; set; }

        public bool IsChecked { get; set; }

        public bool IsAutoCheckingEnabled { get; set; }

        public override void UpdateCeases() {
            base.UpdateCeases();
            IsPressed = false;
            IsClicked = false;
            IsTapped = false;
        }

        public override void Update() {
            WidgetInput input = Input;
            IsPressed = false;
            IsTapped = false;
            IsClicked = false;
            if (input.Press.HasValue
                && HitTestGlobal(input.Press.Value) == this) {
                IsPressed = true;
            }
            if (input.Tap.HasValue
                && HitTestGlobal(input.Tap.Value) == this) {
                IsTapped = true;
            }
            if (input.Click.HasValue
                && HitTestGlobal(input.Click.Value.Start) == this
                && HitTestGlobal(input.Click.Value.End) == this) {
                IsClicked = true;
                if (IsAutoCheckingEnabled) {
                    IsChecked = !IsChecked;
                }
                if (!string.IsNullOrEmpty(SoundName)) {
                    AudioManager.PlaySound(SoundName, 1f, 0f, 0f);
                }
            }
        }
    }
}