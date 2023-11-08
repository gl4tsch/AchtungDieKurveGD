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

        Snake snake;
        enum RebindKey
        {
            None,
            Left,
            Right,
            Fire
        }
        RebindKey waitingForRebindKey = RebindKey.None;
        static readonly List<Key> cancelRebindKeys = new(){ Key.Escape };

        public LobbySnake()
        {
            snake = new Snake()
            {
                Name = "Snake"
            };
        }

        public override void _Ready()
        {
            base._Ready();

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
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (waitingForRebindKey == RebindKey.None)
            {
                return;
            }
            // OnKeyDown
            else if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && !keyEvent.IsEcho())
            {
                if (!cancelRebindKeys.Contains(keyEvent.Keycode))
                {
                    // rebind key attempt
                    switch (waitingForRebindKey)
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
                UpdateControlButtonLabels();
                waitingForRebindKey = RebindKey.None;
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
            rightButton.Text = snake.TurnRightKey.ToString();
            fireButton.Text = snake.FireKey.ToString();
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
            waitingForRebindKey = RebindKey.Left;
        }

        void OnRightButtonClicked()
        {
            waitingForRebindKey = RebindKey.Right;
        }

        void OnFireButtonClicked()
        {
            waitingForRebindKey = RebindKey.Fire;
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
    }
}
