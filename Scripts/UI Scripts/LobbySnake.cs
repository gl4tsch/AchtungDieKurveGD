using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public Snake Snake{ get; private set; }

        enum RebindKey
        {
            None,
            Left,
            Right,
            Fire
        }
        RebindKey awaitedRebindKey = RebindKey.None;
        static readonly List<Key> forbiddenControlKeys = new(){ Key.Escape };

        // List<(string name, Func<Ability> creator)> abilityFactory = new()
        // {
        //     ("None", () => null),
        //     (EraserAbility.DisplayName, () => new EraserAbility()),
        //     (TBarAbility.DisplayName, () => new TBarAbility())
        // };
        List<Ability> allAbilities = new()
        {
            null,
            new EraserAbility(),
            new TBarAbility()
        };

        public LobbySnake Init(Snake snake)
        {
            this.Snake = snake;
            return this;
        }

        public override void _Ready()
        {
            base._Ready();

            if (Snake == null)
            {
                Init(new Snake());
            }

            UpdateNameInputField();
            nameInput.TextChanged += OnSnakeNameInput;

            colorButton.Pressed += OnColorButtonClicked;

            UpdateControlButtonLabels();
            leftButton.Pressed += OnLeftButtonClicked;
            rightButton.Pressed += OnRightButtonClicked;
            fireButton.Pressed += OnFireButtonClicked;

            abilityDD.Clear();
            foreach (var ability in allAbilities) // (string ability in abilityFactory.Select(a => a.name))
            {
                abilityDD.AddItem(ability?.Name ?? "None");
            }
            UpdateDDValue();
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
            //var idx = abilityFactory.FindIndex(a => a.name == (Snake.Ability?.Name ?? "None"));
            int idx = allAbilities.FindIndex(a => a?.Name == Snake.Ability?.Name);
            abilityDD.Select(idx);
        }

        void OnSnakeNameInput(string input)
        {
            Snake.Name = input;
        }

        void OnColorButtonClicked()
        {
            Snake.RandomizeColor();
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
                Snake.Ability = null;
            }
            else if (ddIdx == 1)
            {
                Snake.Ability = new EraserAbility();
            }
            else if (ddIdx == 2)
            {
                Snake.Ability = new TBarAbility();
            }
        }

        void OnDeleteButtonClicked()
        {
            Lobby?.DeleteSnake(this);
        }
    }
}
