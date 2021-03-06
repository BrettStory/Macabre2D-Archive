﻿namespace Macabre2D.UI.Controls.SceneEditing {

    using GalaSoft.MvvmLight.CommandWpf;
    using Macabre2D.UI.Common;
    using Macabre2D.UI.ServiceInterfaces;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class SceneEditorControl : UserControl {

        public SceneEditorControl() {
            this.CenterCameraCommand = new RelayCommand(this.MonoGameService.CenterCamera);
            this.InitializeComponent();
        }

        public ICommand CenterCameraCommand { get; }

        public IComponentService ComponentService { get; } = ViewContainer.Resolve<IComponentService>();

        public IMonoGameService MonoGameService { get; } = ViewContainer.Resolve<IMonoGameService>();

        public ISceneService SceneService { get; } = ViewContainer.Resolve<ISceneService>();

        public IStatusService StatusService { get; } = ViewContainer.Resolve<IStatusService>();
    }
}