using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.UI
{
    public partial class LobbySnake : HBoxContainer
    {
        [Export] LineEdit nameInput;
        [Export] HuePickerButton colorButton;
        [Export] Button leftButton, rightButton, fireButton;
        [Export] OptionButton abilityDD;
        [Export] Button deleteButton;
        [Export] bool deleteButtonEnabled = true;

        public SnakeLobby Lobby;
        public Snake Snake{ get; private set; } = new();

        enum RebindKey
        {
            None,
            Left,
            Right,
            Fire
        }
        RebindKey awaitedRebindKey = RebindKey.None;
        static readonly List<Key> forbiddenControlKeys = new(){ Key.Escape };

        SettingsSection abilitySettings => GameManager.Instance.Settings.AbilitySettings;
        List<(string name, Func<Ability> creator)> abilityFactory => GameManager.Instance.AbilityFactory;

        public LobbySnake Init(Snake snake)
        {
            this.Snake = snake;
            UpdateNameInputField();
            UpdateDDValue();
            return this;
        }

        public override void _Ready()
        {
            base._Ready();

            UpdateNameInputField();
            nameInput.TextChanged += OnSnakeNameInput;

            colorButton.Pressed += () => colorButton.Hue = Snake.Color.H;
            colorButton.HueChanged += OnColorPicked;

            UpdateControlButtonLabels();
            leftButton.Pressed += OnLeftButtonClicked;
            rightButton.Pressed += OnRightButtonClicked;
            fireButton.Pressed += OnFireButtonClicked;

            abilityDD.Clear();
            foreach (var ability in abilityFactory)
            {
                abilityDD.AddItem(ability.name);
            }
            UpdateDDValue();
            // create fresh ability
            OnAbilitySelected(abilityDD.Selected);
            abilityDD.ItemSelected += OnAbilitySelected;

            deleteButton.Pressed += OnDeleteButtonClicked;
            deleteButton.Visible = deleteButtonEnabled;
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (awaitedRebindKey == RebindKey.None)
            {
                return;
            }
            // OnKeyDown
            else if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && !keyEvent.IsEcho())
            {
                if (!forbiddenControlKeys.Contains(keyEvent.Keycode))
                {
                    // rebind key attempt
                    switch (awaitedRebindKey)
                    {
                        case RebindKey.Left:
                            Snake.TurnLeftKey = keyEvent.Keycode;
                            break;
                        case RebindKey.Right:
                            Snake.TurnRightKey = keyEvent.Keycode;
                            break;
                        case RebindKey.Fire:
                            Snake.FireKey = keyEvent.Keycode;
                            break;
                    }
                }
                awaitedRebindKey = RebindKey.None;
                UpdateControlButtonLabels();
            }
        }

        void UpdateNameInputField()
        {
            nameInput.Text = Snake.Name;
            nameInput.AddThemeColorOverride("font_color", Snake.Color);
        }

        void UpdateControlButtonLabels()
        {
            leftButton.Text = Snake.TurnLeftKey.ToString();
            SetControlButtonState(leftButton, awaitedRebindKey == RebindKey.Left);

            rightButton.Text = Snake.TurnRightKey.ToString();
            SetControlButtonState(rightButton, awaitedRebindKey == RebindKey.Right);

            fireButton.Text = Snake.FireKey.ToString();
            SetControlButtonState(fireButton, awaitedRebindKey == RebindKey.Fire);
        }

        void SetControlButtonState(Button button, bool awaitingKey)
        {
            if (awaitingKey)
            {
                button.AddThemeColorOverride("font_color", new Color(1, 0, 0));
                button.AddThemeColorOverride("font_hover_color", new Color(1, 0, 0));
            }
            else
            {
                button.RemoveThemeColorOverride("font_color");
                button.RemoveThemeColorOverride("font_hover_color");
            }
        }

        void UpdateDDValue()
        {
            var idx = abilityFactory.FindIndex(a => a.name == (Snake.Ability?.Name ?? Ability.NoAbilityDisplayName));
            //int idx = allAbilities.FindIndex(a => a?.Name == Snake.Ability?.Name);
            abilityDD.Select(idx);
        }

        void OnSnakeNameInput(string input)
        {
            Snake.Name = input;
        }

        void OnColorPicked(float hue)
        {
            Snake.Color = Color.FromHsv(hue, 1, 1);
            UpdateNameInputField();
        }

        void OnLeftButtonClicked()
        {
            awaitedRebindKey = RebindKey.Left;
            UpdateControlButtonLabels();
        }

        void OnRightButtonClicked()
        {
            awaitedRebindKey = RebindKey.Right;
            UpdateControlButtonLabels();
        }

        void OnFireButtonClicked()
        {
            awaitedRebindKey = RebindKey.Fire;
            UpdateControlButtonLabels();
        }

        void OnAbilitySelected(long ddIdx)
        {
            int idx = (int)ddIdx;
            Snake.Ability = abilityFactory[idx].creator();
        }

        void OnDeleteButtonClicked()
        {
            Lobby?.DeleteSnake(this);
        }
    }
}
