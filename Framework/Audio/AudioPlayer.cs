﻿namespace Macabre2D.Framework {

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Audio;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Plays a <see cref="AudioClip"/>.
    /// </summary>
    public sealed class AudioPlayer : BaseComponent, IAssetComponent<AudioClip> {

        [DataMember(Order = 0, Name = "Audio Clip")]
        private AudioClip _audioClip;

        [DataMember(Order = 3, Name = "Pan")]
        private float _pan;

        [DataMember(Order = 4, Name = "Pitch")]
        private float _pitch;

        [DataMember(Order = 1, Name = "Should Loop")]
        private bool _shouldLoop;

        [DataMember(Order = 2, Name = "Volume")]
        private float _volume;

        /// <summary>
        /// Gets or sets the audio clip.
        /// </summary>
        /// <value>The audio clip.</value>
        public AudioClip AudioClip {
            get {
                return this._audioClip;
            }

            set {
                if (this._audioClip != value) {
                    this._audioClip?.SoundEffectInstance?.Stop(true);
                    this._audioClip = value;
                    this.LoadContent();
                }
            }
        }

        /// <summary>
        /// Gets or sets the pan.
        /// </summary>
        /// <value>The pan.</value>
        public float Pan {
            get {
                return this._pan;
            }

            set {
                this._pan = MathHelper.Clamp(value, -1f, 1f);
                if (this._audioClip?.SoundEffectInstance != null) {
                    this._audioClip.SoundEffectInstance.Pan = this._pan;
                }
            }
        }

        /// <summary>
        /// Gets or sets the pitch.
        /// </summary>
        /// <value>The pitch.</value>
        public float Pitch {
            get {
                return this._pitch;
            }

            set {
                this._pitch = MathHelper.Clamp(value, -1f, 1f);
                if (this._audioClip?.SoundEffectInstance != null) {
                    this._audioClip.SoundEffectInstance.Pitch = this._pitch;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AudioPlayer"/> should loop.
        /// </summary>
        /// <value><c>true</c> if should loop; otherwise, <c>false</c>.</value>
        public bool ShouldLoop {
            get {
                return this._shouldLoop;
            }

            set {
                this._shouldLoop = value;
                if (this._audioClip?.SoundEffectInstance != null) {
                    this._audioClip.SoundEffectInstance.IsLooped = this._shouldLoop;
                }
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public SoundState State {
            get {
                return this._audioClip.SoundEffectInstance?.State ?? SoundState.Stopped;
            }
        }

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public float Volume {
            get {
                return this._volume;
            }

            set {
                this._volume = MathHelper.Clamp(value, 0f, 1f);
                if (this._audioClip?.SoundEffectInstance != null) {
                    this._audioClip.SoundEffectInstance.Volume = this._volume;
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Guid> GetOwnedAssetIds() {
            return this.AudioClip != null ? new[] { this.AudioClip.Id } : new Guid[0];
        }

        /// <inheritdoc/>
        public bool HasAsset(Guid id) {
            return this.AudioClip?.Id == id;
        }

        /// <inheritdoc/>
        public override void LoadContent() {
            if (this._audioClip != null && this.Scene.IsInitialized) {
                this._audioClip.LoadSoundEffect(this.Volume, this.Pan, this.Pitch);
            }

            base.LoadContent();
        }

        /// <summary>
        /// Pause this instance.
        /// </summary>
        public void Pause() {
            if (this._audioClip?.SoundEffectInstance != null && this._audioClip.SoundEffectInstance.State == SoundState.Playing) {
                this._audioClip.SoundEffectInstance.Pause();
            }
        }

        /// <summary>
        /// Play this instance.
        /// </summary>
        public void Play() {
            if (this._audioClip?.SoundEffectInstance != null && this._audioClip.SoundEffectInstance.State != SoundState.Playing) {
                this._audioClip.SoundEffectInstance.Play();
            }
        }

        /// <inheritdoc/>
        public void RefreshAsset(Guid currentId, AudioClip newAsset) {
            if (this.AudioClip == null || this.AudioClip.Id == currentId) {
                this.AudioClip = newAsset;
            }
        }

        public void RefreshAsset(AudioClip newInstance) {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool RemoveAsset(Guid id) {
            var result = this.HasAsset(id);
            if (result) {
                this._audioClip = null;
            }

            return result;
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        public void Stop() {
            if (this._audioClip?.SoundEffectInstance != null && this._audioClip.SoundEffectInstance.State != SoundState.Stopped) {
                this._audioClip.SoundEffectInstance.Stop();
            }
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        /// <param name="isImmediate">If set to <c>true</c> is immediate.</param>
        public void Stop(bool isImmediate) {
            if (this._audioClip?.SoundEffectInstance != null && this._audioClip.SoundEffectInstance.State != SoundState.Stopped) {
                this._audioClip.SoundEffectInstance.Stop(isImmediate);
            }
        }

        /// <inheritdoc/>
        public bool TryGetAsset(Guid id, out AudioClip asset) {
            var result = this.AudioClip?.Id == id;
            asset = result ? this.AudioClip : null;
            return result;
        }

        /// <inheritdoc/>
        protected override void Initialize() {
            return;
        }
    }
}