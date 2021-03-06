﻿namespace Macabre2D.UI.Views {

    using Macabre2D.UI.Common;
    using Macabre2D.UI.ViewModels;
    using System.Windows.Controls;

    public partial class ProjectView : UserControl {

        public ProjectView() {
            this.DataContext = ViewContainer.Resolve<ProjectViewModel>();
            this.InitializeComponent();
        }
    }
}