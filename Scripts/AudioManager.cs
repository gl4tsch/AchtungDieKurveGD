using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public partial class AudioManager : Node
    {
        public static AudioManager Instance { get; private set; }

        [ExportCategory("Music")]
        [Export] AudioStream lobbyMusic;
        [Export] AudioStream battleMusic;

        [ExportCategory("Sound")]
        [Export] AudioStream snakeDeathExplosionSound;
        [Export] AudioStream barAbilitySound;
        [Export] AudioStream teleportAbilitySound;
        [Export] AudioStream speedAbilitySound;
        [Export] AudioStream eraserAbilitySound;

        StringName musicBus = "Musc Bus";
        StringName soundBus = "Sound Bus";

        Dictionary<Music, AudioStream> musicFiles;
        Dictionary<SFX, AudioStream> soundFiles;

        AudioStreamPlayer musicPlayer;
        Dictionary<SFX, AudioStreamPlayer> soundPlayers = new();

        public AudioManager()
        {
            Instance = this;
        }

        public override void _Ready()
        {
            base._Ready();
            
            // Music
            musicFiles = new()
            {
                {Music.LobbyTheme, lobbyMusic},
                {Music.BattleTheme, battleMusic}
            };

            // Sound Effects
            soundFiles = new()
            {
                {SFX.SnakeDeathExplosion, snakeDeathExplosionSound},
                {SFX.BarAbility, barAbilitySound},
                {SFX.SpeedAbility, speedAbilitySound},
                {SFX.TeleportAbility, teleportAbilitySound},
                {SFX.EraserAbility, eraserAbilitySound},
            };

            // init music player
            musicPlayer = new AudioStreamPlayer
            {
                Bus = musicBus
            };
            AddChild(musicPlayer);

            // init sound players
            foreach (var soundFile in soundFiles)
            {
                var sfxPlayer = new AudioStreamPlayer
                {
                    Stream = soundFile.Value,
                    Bus = soundBus
                };
                AddChild(sfxPlayer);
                soundPlayers.Add(soundFile.Key, sfxPlayer);
            }
        }

        public void PlayMusic(Music music)
        {
            if (musicFiles.TryGetValue(music, out var stream))
            {
                PlayMusic(stream);
            }
            else
            {
                PauseMusic();
            }
        }

        public void PlayMusic(AudioStream musicStream)
        {
            if (musicPlayer == null)
            {
                return;
            }

            musicPlayer.Stream = musicStream;
            musicPlayer.Play();
        }

        public void PauseMusic()
        {
            if (musicPlayer == null)
            {
                return;
            }

            musicPlayer.StreamPaused = true;
        }

        public void UnPauseMusic()
        {
            if (musicPlayer == null)
            {
                return;
            }

            musicPlayer.StreamPaused = false;
        }

        public void StopMusic()
        {
            musicPlayer.Stop();
        }

        public void PlaySound(SFX sound)
        {
            if (soundPlayers.TryGetValue(sound, out var player))
            {
                player.Play();
            }
            else
            {
                GD.PrintErr("No sound found for " + sound);
            }
        }
    }

    public enum Music
    {
        LobbyTheme,
        BattleTheme
    }

    public enum SFX
    {
        SnakeDeathExplosion,
        BarAbility,
        SpeedAbility,
        TeleportAbility,
        EraserAbility
    }
}