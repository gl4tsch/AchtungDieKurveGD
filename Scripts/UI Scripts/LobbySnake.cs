using Godot;
using System;
using System.Collections.Generic;

namespace ADK.UI
{
    public partial class LobbySnake : HBoxContainer
    {
        [Export] LineEdit nameInput;
        [Export] Button colorButton;
        [Export] Button leftButton, rightButton, fireButton;
        [Export] OptionButton abilityDD;
        [Export] Button deleteButton;

        public SnakeLobby Lobby;

        Snake snake;
        enum RebindKey
        {
            None,
            Left,
            Right,
            Fire
        }
        RebindKey awaitedRebindKey = RebindKey.None;
        static readonly List<Key> cancelRebindKeys = new(){ Key.Escape };

        public override void _Ready()
        {
            base._Ready();

            snake = new Snake()
            {
                Name = "Snake"
            };

            UpdateNameInputField();
            nameInput.TextChanged += OnSnakeNameInput;

            colorButton.Pressed += OnColorButtonClicked;

            UpdateControlButtonLabels();
            leftButton.Pressed += OnLeftButtonClicked;
            rightButton.Pressed += OnRightButtonClicked;
            fireButton.Pressed += OnFireButtonClicked;

            abilityDD.Clear();
            abilityDD.AddItem("None");
            abilityDD.AddItem(EraserAbility.DisplayName);
            abilityDD.AddItem(TBarAbility.DisplayName);
            abilityDD.ItemSelected += OnAbilitySelected;

            deleteButton.Pressed += OnDeleteButtonClicked;
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
                if (!cancelRebindKeys.Contains(keyEvent.Keycode))
                {
                    // rebind key attempt
                    switch (awaitedRebindKey)
                    {
                        case RebindKey.Left:
                            snake.TurnLeftKey = keyEvent.Keycode;
                            break;
                        case RebindKey.Right:
                            snake.TurnRightKey = keyEvent.Keycode;
                            break;
                        case RebindKey.Fire:
                            snake.FireKey = keyEvent.Keycode;
                            break;
                    }
                }
                awaitedRebindKey = RebindKey.None;
                UpdateControlButtonLabels();
            }
        }

        void UpdateNameInputField()
        {
            nameInput.Text = snake.Name;
            nameInput.AddThemeColorOverride("font_color", snake.Color);
        }

        void UpdateControlButtonLabels()
        {
            leftButton.Text = snake.TurnLeftKey.ToString();
            SetControlButtonState(leftButton, awaitedRebindKey == RebindKey.Left);

            rightButton.Text = snake.TurnRightKey.ToString();
            SetControlButtonState(rightButton, awaitedRebindKey == RebindKey.Right);

            fireButton.Text = snake.FireKey.ToString();
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

        void OnSnakeNameInput(string input)
        {
            snake.Name = input;
        }

        void OnColorButtonClicked()
        {
            snake.RandomizeColor();
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
            if (ddIdx == 0)
            {
                snake.Ability = null;
            }
            else if (ddIdx == 1)
            {
                snake.Ability = new EraserAbility();
            }
            else if (ddIdx == 2)
            {
                snake.Ability = new TBarAbility();
            }
        }

        void OnDeleteButtonClicked()
        {
            Lobby?.DeleteSnake(this);
        }
    }
}
