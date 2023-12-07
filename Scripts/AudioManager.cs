using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public partial class AudioManager : Node
    {
        public static AudioManager Instance { get; private set; }

        [ExportCategory("Music")]
        [Export] Godot.Collections.Array<AudioStream> lobbyMusic;
        [Export] Godot.Collections.Array<AudioStream> battleMusic;

        [ExportCategory("Sound")]
        [Export] AudioStream snakeDeathExplosionSound;
        [Export] AudioStream barAbilitySound;
        [Export] AudioStream teleportAbilitySound;
        [Export] AudioStream speedAbilitySound;
        [Export] AudioStream eraserAbilitySound;

        int masterBusIdx = 0;
        StringName musicBus = "Music Bus";
        int musicBusIdx => AudioServer.GetBusIndex(musicBus);
        StringName soundBus = "SoundFX Bus";
        int soundBusIdx => AudioServer.GetBusIndex(soundBus);

        Dictionary<Music, Godot.Collections.Array<AudioStream>> musicFiles;
        Dictionary<SFX, AudioStream> soundFiles;

        AudioStreamPlayer musicPlayer;
        Dictionary<SFX, AudioStreamPlayer> soundPlayers = new();

        float minDb = -60;
        float maxDb = 0;

        /// <summary>
        /// [0, 100]
        /// </summary>
        public float MasterVolume => AudioServer.GetBusVolumeDb(masterBusIdx).Map(minDb, maxDb, 0, 100);
        /// <summary>
        /// [0, 100]
        /// </summary>
        public float MusicVolume => AudioServer.GetBusVolumeDb(musicBusIdx).Map(minDb, maxDb, 0, 100);
        /// <summary>
        /// [0, 100]
        /// </summary>
        public float SoundVolume => AudioServer.GetBusVolumeDb(soundBusIdx).Map(minDb, maxDb, 0, 100);

        public static Dictionary<string, Variant> DefaultSettings => new()
        {
            {nameof(MasterVolume), 100f},
            {nameof(MusicVolume), 100f},
            {nameof(SoundVolume), 100f}
        };

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

            ApplySettings(GameManager.Instance.Settings.AudioSettings);
        }

        public void ApplySettings(SettingsSection settings)
        {
            if (settings.Settings.TryGetValue(nameof(MasterVolume), out var masterVolume))
            {
                SetMasterVolume((float)masterVolume);
            }
            if (settings.Settings.TryGetValue(nameof(MusicVolume), out var musicVolume))
            {
                SetMusicVolume((float)musicVolume);
            }
            if (settings.Settings.TryGetValue(nameof(SoundVolume), out var soundVolume))
            {
                SetSoundVolume((float)soundVolume);
            }
        }

        public void PlayMusic(Music music)
        {
            if (musicFiles.TryGetValue(music, out var list))
            {
                PlayMusic(list.PickRandom());
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

        /// <param name="volume">[0, 100]</param>
        public void SetMasterVolume(float volume)
        {
            AudioServer.SetBusVolumeDb(masterBusIdx, volume.Map(0,100,minDb, maxDb));
        }

        /// <param name="volume">[0, 100]</param>
        public void SetMusicVolume(float volume)
        {
            AudioServer.SetBusVolumeDb(musicBusIdx, volume.Map(0, 100, minDb, maxDb));
        }

        /// <param name="volume">[0, 100]</param>
        public void SetSoundVolume(float volume)
        {
            AudioServer.SetBusVolumeDb(soundBusIdx, volume.Map(0, 100, minDb, maxDb));
        }

        public void MuteMaster(bool mute)
        {
            AudioServer.SetBusMute(masterBusIdx, mute);
        }

        public void MuteMusic(bool mute)
        {
            AudioServer.SetBusMute(musicBusIdx, mute);
        }

        public void MuteSounds(bool mute)
        {
            AudioServer.SetBusMute(soundBusIdx, mute);
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