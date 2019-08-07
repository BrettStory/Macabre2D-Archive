﻿namespace Macabre2D.UI.Models {

    using Macabre2D.Framework;
    using Macabre2D.UI.Common;
    using Macabre2D.UI.Models.FrameworkWrappers;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class AutoTileSetAsset : AddableAsset<AutoTileSet> {
        private readonly ObservableRangeCollection<IndexedWrapper<SpriteWrapper>> _indexedSprites = new ObservableRangeCollection<IndexedWrapper<SpriteWrapper>>();

        public override string FileExtension {
            get {
                return FileHelper.AutoTileSetExtension;
            }
        }

        public IReadOnlyCollection<IndexedWrapper<SpriteWrapper>> IndexedSprites {
            get {
                return this._indexedSprites;
            }
        }

        public override AssetType Type {
            get {
                return AssetType.AutoTileSet;
            }
        }

        public bool UseIntermediateDirections {
            get {
                return this.SavableValue.UseIntermediateDirections;
            }

            set {
                if (this.UseIntermediateDirections != value) {
                    this.SavableValue.UseIntermediateDirections = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public override void Refresh(AssetManager assetManager) {
            base.Refresh(assetManager);

            var spritesToAdd = new IndexedWrapper<SpriteWrapper>[this.SavableValue.Size];
            var root = this.GetRootFolder();
            var spriteWrappers = root.GetAssetsOfType<SpriteWrapper>().Where(x => x.Sprite != null).ToList();

            for (byte i = 0; i < spritesToAdd.Length; i++) {
                var sprite = this.SavableValue.GetSprite(i);
                var spriteWrapper = spriteWrappers.FirstOrDefault(x => x.Sprite.Id == sprite?.Id);
                var indexedWrapper = this._indexedSprites.FirstOrDefault(x => x.Index == i);

                if (indexedWrapper == null) {
                    indexedWrapper = new IndexedWrapper<SpriteWrapper>(spriteWrapper, i);
                    indexedWrapper.PropertyChanged += this.IndexedWrapper_PropertyChanged;
                }
                else {
                    indexedWrapper.WrappedObject = spriteWrapper;
                }

                spritesToAdd[i] = indexedWrapper;
            }

            this._indexedSprites.Reset(spritesToAdd);
        }

        private void IndexedWrapper_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (this.SavableValue != null && sender is IndexedWrapper<SpriteWrapper> indexedWrapper && e.PropertyName == nameof(IndexedWrapper<SpriteWrapper>.WrappedObject)) {
                this.SavableValue.SetSprite(indexedWrapper.WrappedObject?.Sprite, (byte)indexedWrapper.Index);
                this.RaisePropertyChanged(nameof(this.IndexedSprites));
            }
        }
    }
}