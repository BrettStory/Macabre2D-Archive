﻿namespace Macabre2D.Framework {

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an in-game scene (level, menu screen, loading screen, cinematic, etc).
    /// </summary>
    [DataContract]
    public sealed class Scene : IScene, IDisposable {
        private static readonly Action<BaseModule, GameTime> ModulePostUpdateAction = (module, gameTime) => module.PostUpdate(gameTime);
        private static readonly Action<BaseModule, GameTime> ModulePreUpdateAction = (module, gameTime) => module.PreUpdate(gameTime);
        private static readonly Action<IUpdateableComponent, GameTime> UpdateAction = (updateable, gameTime) => updateable.Update(gameTime);
        private static readonly Func<IUpdateableComponentAsync, GameTime, Task> UpdateAsyncAction = (updateableAsync, gameTime) => updateableAsync.UpdateAsync(gameTime);
        private readonly NotifyCollectionChangedEventHandler _componentChildrenChangedHandler;
        private readonly List<BaseComponent> _components = new List<BaseComponent>();
        private readonly Dictionary<Type, object> _dependencies = new Dictionary<Type, object>();

        private readonly FilterSortCollection<IDrawableComponent> _drawables = new FilterSortCollection<IDrawableComponent>(
            d => d.IsVisible,
            (d, handler) => d.IsVisibleChanged += handler,
            (d, handler) => d.IsVisibleChanged -= handler,
            (d1, d2) => Comparer<int>.Default.Compare(d1.DrawOrder, d2.DrawOrder),
            (d, handler) => d.DrawOrderChanged += handler,
            (d, handler) => d.DrawOrderChanged -= handler);

        private readonly QuadTree<IDrawableComponent> _drawTree = new QuadTree<IDrawableComponent>(0, float.MinValue * 0.5f, float.MinValue * 0.5f, float.MaxValue, float.MaxValue);
        private readonly List<Action<GameTime>> _endOfFrameActions = new List<Action<GameTime>>();

        private readonly FilterSortCollection<BaseModule> _modules = new FilterSortCollection<BaseModule>(
            m => m.IsEnabled,
            (m, handler) => m.IsEnabledChanged += handler,
            (m, handler) => m.IsEnabledChanged -= handler,
            (m1, m2) => Comparer<int>.Default.Compare(m1.UpdateOrder, m2.UpdateOrder),
            (m, handler) => m.UpdateOrderChanged += handler,
            (m, handler) => m.UpdateOrderChanged -= handler);

        private readonly FilterCollection<IUpdateableComponentAsync> _updateableAsyncs = new FilterCollection<IUpdateableComponentAsync>(
            u => u.IsEnabled,
            (u, handler) => u.IsEnabledChanged += handler,
            (u, handler) => u.IsEnabledChanged -= handler);

        private readonly FilterSortCollection<IUpdateableComponent> _updateables = new FilterSortCollection<IUpdateableComponent>(
            u => u.IsEnabled,
            (u, handler) => u.IsEnabledChanged += handler,
            (u, handler) => u.IsEnabledChanged -= handler,
            (u1, u2) => Comparer<int>.Default.Compare(u1.UpdateOrder, u2.UpdateOrder),
            (u, handler) => u.UpdateOrderChanged += handler,
            (u, handler) => u.UpdateOrderChanged -= handler);

        private bool _disposedValue;
        private int _sessionIdCounter = int.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        public Scene() {
            this._componentChildrenChangedHandler = new NotifyCollectionChangedEventHandler(this.Component_ChildrenChanged);
        }

        /// <inheritdoc/>
        public event EventHandler<BaseComponent> ComponentCreated;

        /// <inheritdoc/>
        public event EventHandler<BaseComponent> ComponentDestroyed;

        /// <inheritdoc/>
        public event EventHandler<BaseModule> ModuleAdded;

        /// <inheritdoc/>
        public event EventHandler<BaseModule> ModuleRemoved;

        /// <inheritdoc/>
        [DataMember]
        public Guid AssetId { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public Color BackgroundColor { get; set; } = Color.Black;

        /// <inheritdoc/>
        public IReadOnlyCollection<BaseComponent> Components {
            get {
                return this._components;
            }
        }

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        [DataMember]
        public string Name { get; set; }

        internal FilterSortCollection<Camera> Cameras { get; } = new FilterSortCollection<Camera>(
            c => c.IsEnabled,
            (c, handler) => c.IsEnabledChanged += handler,
            (c, handler) => c.IsEnabledChanged -= handler,
            (c1, c2) => Comparer<int>.Default.Compare(c1.RenderOrder, c2.RenderOrder),
            (c, handler) => c.RenderOrderChanged += handler,
            (c, handler) => c.RenderOrderChanged -= handler);

        [DataMember]
        internal HashSet<BaseComponent> ComponentsForSaving { get; } = new HashSet<BaseComponent>();

        [DataMember]
        internal HashSet<BaseModule> ModulesForSaving { get; } = new HashSet<BaseModule>();

        /// <inheritdoc/>
        public T AddComponent<T>() where T : BaseComponent, new() {
            var component = new T { IsEnabled = true };
            this.AddComponent(component);
            return component;
        }

        /// <inheritdoc/>
        public bool AddComponent(BaseComponent component) {
            if (component == null) {
                throw new ArgumentNullException(nameof(component));
            }

            if (!this.IsInitialized) {
                if (!this.ComponentsForSaving.Any(x => x.Id == component.Id)) {
                    this.ComponentsForSaving.Add(component);
                }

                return false;
            }

            if (!this._components.Any(x => x.Id == component.Id)) {
                this._components.Add(component);
                this.TrackComponent(component);
            }

            return true;
        }

        // <inheritdoc/>
        public bool AddModule(BaseModule module) {
            if (!this.IsInitialized) {
                if (!this.ModulesForSaving.Any(x => x.Id == module.Id)) {
                    this.ModulesForSaving.Add(module);
                }

                return false;
            }

            this._modules.Add(module);

            module.Initialize(this);
            module.PreInitialize();
            module.PostInitialize();
            this.ModuleAdded.SafeInvoke(this, module);
            return true;
        }

        /// <inheritdoc/>
        public bool AddModule(FixedTimeStepModule module, float timeStep) {
            module.TimeStep = timeStep;
            return this.AddModule(module);
        }

        /// <inheritdoc/>
        public T CreateModule<T>() where T : BaseModule, new() {
            var module = new T();
            this.AddModule(module);
            return module;
        }

        /// <inheritdoc/>
        public T CreateModule<T>(float timeStep) where T : FixedTimeStepModule, new() {
            var module = this.CreateModule<T>();
            this.AddModule(module, timeStep);
            return module;
        }

        /// <inheritdoc/>
        public void DestroyComponent(BaseComponent component) {
            this.RemoveComponent(component);
            this.ComponentDestroyed.SafeInvoke(this, component);
            component.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Draw(GameTime gameTime) {
            this._drawTree.Clear();
            this._drawables.ForEachFilteredItem(d => this._drawTree.Insert(d));

            this.Cameras.ForEachFilteredItem(camera => {
                this.DrawForCamera(gameTime, camera);
            });
        }

        /// <inheritdoc/>
        public void Draw(GameTime gameTime, params Camera[] cameras) {
            this._drawTree.Clear();
            this._drawables.ForEachFilteredItem(d => this._drawTree.Insert(d));

            foreach (var camera in cameras) {
                this.DrawForCamera(gameTime, camera);
            }
        }

        /// <inheritdoc/>
        public BaseComponent FindComponent(string name) {
            foreach (var component in this.Components) {
                if (string.Equals(name, component.Name)) {
                    return component;
                }

                var found = component.FindComponentInChildren(name);
                if (found != null) {
                    return found;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public T FindComponentOfType<T>() where T : BaseComponent {
            foreach (var component in this.Components) {
                if (component is T componentOfType) {
                    return componentOfType;
                }

                var result = component.GetComponentInChildren<T>(true);
                if (result != null) {
                    return result;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Gets all components in this scene.
        /// </summary>
        /// <param name="includeComponentsForSaving">
        /// if set to <c>true</c> [include components for saving].
        /// </param>
        /// <returns>All components in this scene.</returns>
        public IEnumerable<BaseComponent> GetAllComponents(bool includeComponentsForSaving, bool includeNestedComponents) {
            var components = new List<BaseComponent>();

            foreach (var component in this.Components) {
                components.Add(component);

                if (includeNestedComponents) {
                    components.AddRange(component.GetAllChildren());
                }
            }

            if (includeComponentsForSaving) {
                foreach (var component in this.ComponentsForSaving) {
                    components.Add(component);

                    if (includeNestedComponents) {
                        components.AddRange(component.GetAllChildren());
                    }
                }
            }

            return components;
        }

        /// <inheritdoc/>
        public List<T> GetAllComponentsOfType<T>() {
            var components = new List<T>();

            foreach (var component in this._components) {
                if (component is T specifiedComponent) {
                    components.Add(specifiedComponent);
                }

                components.AddRange(component.GetComponentsInChildren<T>());
            }

            foreach (var component in this.ComponentsForSaving) {
                if (component is T specifiedComponent) {
                    components.Add(specifiedComponent);
                }

                components.AddRange(component.GetComponentsInChildren<T>());
            }

            return components;
        }

        /// <summary>
        /// Gets all modules in this scene.
        /// </summary>
        /// <param name="includeModulesForSaving">if set to <c>true</c> [include modules for saving].</param>
        /// <returns>All modules in this scene.</returns>
        public IEnumerable<BaseModule> GetAllModules(bool includeModulesForSaving) {
            var modules = new List<BaseModule>();

            this._modules.RebuildCache();
            foreach (var module in this._modules) {
                modules.Add(module);
            }

            if (includeModulesForSaving) {
                foreach (var module in this.ModulesForSaving) {
                    modules.Add(module);
                }
            }

            return modules;
        }

        /// <inheritdoc/>
        public T GetModule<T>() where T : BaseModule {
            this._modules.RebuildCache();
            return (T)this._modules.FirstOrDefault(x => x.GetType() == typeof(T));
        }

        /// <inheritdoc/>
        public IEnumerable<T> GetModules<T>() where T : BaseModule {
            this._modules.RebuildCache();
            return this._modules.OfType<T>();
        }

        /// <inheritdoc/>
        public IEnumerable<IDrawableComponent> GetVisibleDrawableComponents() {
            return this._drawables;
        }

        /// <inheritdoc/>
        public void Initialize() {
            if (!this.IsInitialized) {
                this.IsInitialized = true;
                this._modules.AddRange(this.ModulesForSaving);
                this._modules.RebuildCache();
                this.ModulesForSaving.Clear();

                foreach (var module in this._modules) {
                    module.Initialize(this);
                    module.PreInitialize();
                }

                foreach (var component in this.ComponentsForSaving) {
                    this.AddComponent(component);
                }

                this.ComponentsForSaving.Clear();

                // Rebuild, just in case a component's initialize or another module's preinitialize
                // changed the collection.
                this._modules.RebuildCache();
                foreach (var module in this._modules) {
                    module.PostInitialize();
                }
            }
        }

        /// <inheritdoc/>
        public void LoadContent() {
            foreach (var child in this.Components) {
                child.LoadContent();
            }
        }

        /// <inheritdoc/>
        public void QueueEndOfFrameAction(Action<GameTime> action) {
            this._endOfFrameActions.Add(action);
        }

        /// <inheritdoc/>
        public void RemoveModule(BaseModule module) {
            if (this._modules.Remove(module)) {
                this.ModuleRemoved.SafeInvoke(this, module);
            }
            else {
                this.ModulesForSaving.Remove(module);
            }

            if (module is IDisposable disposable) {
                disposable.Dispose();
            }
        }

        /// <inheritdoc/>
        public T ResolveDependency<T>() where T : new() {
            if (this._dependencies.TryGetValue(typeof(T), out var dependency)) {
                return (T)dependency;
            }

            dependency = new T();
            this._dependencies.Add(typeof(T), dependency);
            return (T)dependency;
        }

        /// <inheritdoc/>
        public T ResolveDependency<T>(Func<T> objectFactory) {
            if (this._dependencies.TryGetValue(typeof(T), out var dependency)) {
                return (T)dependency;
            }

            dependency = objectFactory.SafeInvoke();
            this._dependencies.Add(typeof(T), dependency);
            return (T)dependency;
        }

        /// <summary>
        /// Saves this scene to a file with the specified file name using the specified <see cref="ISerializer"/>
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        public void SaveToFile(string filePath) {
            try {
                this._modules.RebuildCache();
                this.ModulesForSaving.AddRange(this._modules);
                this.ComponentsForSaving.AddRange(this._components);
                this.Name = Path.GetFileNameWithoutExtension(filePath);
                Serializer.Instance.Serialize(this, filePath);
            }
            finally {
                // We never want to get to a state where this collection has anything after
                // attempting to save. this.ModulesForSaving.Clear();
            }
        }

        /// <inheritdoc/>
        public void Update(GameTime gameTime) {
            this._modules.ForEachFilteredItem(Scene.ModulePreUpdateAction, gameTime);

            var task = this._updateableAsyncs.ForeachEachFilteredItemAsync(Scene.UpdateAsyncAction, gameTime);
            this._updateables.ForEachFilteredItem(Scene.UpdateAction, gameTime);
            task.Wait();

            foreach (var action in this._endOfFrameActions) {
                action.SafeInvoke(gameTime);
            }

            this._modules.ForEachFilteredItem(Scene.ModulePostUpdateAction, gameTime);
        }

        internal bool RemoveChild(BaseComponent component) {
            return this._components.Remove(component) || this.ComponentsForSaving.Remove(component);
        }

        internal void RemoveComponent(BaseComponent component) {
            this.RemoveChild(component);
            this._updateables.Remove(component);
            this._updateableAsyncs.Remove(component);
            this._drawables.Remove(component);
            this.Cameras.Remove(component);

            foreach (var child in component.Children) {
                this.RemoveComponent(child);
            }
        }

        private void Component_ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Add) {
                foreach (var component in e.NewItems.OfType<BaseComponent>()) {
                    if (this.IsInitialized) {
                        this.TrackComponent(component);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove) {
                foreach (var component in e.OldItems.OfType<BaseComponent>()) {
                    this.RemoveComponent(component);
                }
            }
        }

        private void Component_ParentChanged(object sender, BaseComponent e) {
            if (sender is BaseComponent baseComponent) {
                if (e == null) {
                    if (!this._components.Any(x => x.Id == baseComponent.Id)) {
                        this._components.Add(baseComponent);
                    }
                }
                else {
                    this._components.Remove(baseComponent);
                }
            }
        }

        private void Dispose(bool disposing) {
            if (!this._disposedValue) {
                if (disposing) {
                    foreach (var component in this._components) {
                        component.Dispose();
                    }

                    // If any modules are disposable, dispose them
                    foreach (var module in this._modules) {
                        if (module is IDisposable disposable) {
                            disposable.Dispose();
                        }
                    }
                }

                this.ComponentCreated = null;
                this.ComponentDestroyed = null;
                this._disposedValue = true;
            }
        }

        private void DrawForCamera(GameTime gameTime, Camera camera) {
            var potentialDrawables = this._drawTree.RetrievePotentialCollisions(camera.BoundingArea);

            if (potentialDrawables.Any()) {
                MacabreGame.Instance.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, camera.SamplerState, null, RasterizerState.CullNone, camera.Shader?.Effect, camera.ViewMatrix);

                foreach (var drawable in potentialDrawables) {
                    // As long as it doesn't equal Layers.None, at least one of the layers defined on
                    // the component are also to be rendered by LayersToRender.
                    if ((drawable.Layers & camera.LayersToRender) != Layers.None) {
                        drawable.Draw(gameTime, camera.BoundingArea);
                    }
                }

                MacabreGame.Instance.SpriteBatch.End();
            }
        }

        private void TrackComponent(BaseComponent component) {
            this._updateables.Add(component);
            this._updateableAsyncs.Add(component);
            this._drawables.Add(component);
            this.Cameras.Add(component);

            if (component.Parent == null) {
                component.Initialize(this);
                component.LoadContent();
            }

            foreach (var child in component.Children) {
                this.TrackComponent(child);
            }

            component.ParentChanged += this.Component_ParentChanged;
            component.SubscribeToChildrenChanged(this._componentChildrenChangedHandler);
            component.SessionId = Interlocked.Increment(ref this._sessionIdCounter);
            this.ComponentCreated.SafeInvoke(this, component);
        }
    }
}