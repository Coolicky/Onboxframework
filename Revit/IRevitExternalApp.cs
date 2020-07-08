﻿using Autodesk.Revit.UI;
using Onbox.Abstractions.V7;
using Onbox.Revit.V7.UI;

namespace Onbox.Revit.V7
{
    /// <summary>
    /// Onbox contract that wraps Revit's <see cref="IExternalApplication"/>.
    /// </summary>
    public interface IRevitExternalApp: IExternalApplication
    {
        /// <summary>
        /// Lifecycle hook to create Ribbon UI when Revit starts.
        /// </summary>
        void OnCreateRibbon(IRibbonManager ribbonManager);

        /// <summary>
        /// Implement this method to execute some tasks when Autodesk Revit shuts down.
        /// </summary>
        Result OnShutdown(IContainerResolver container, UIControlledApplication application);

        /// <summary>
        /// Implement this method to execute some tasks when Autodesk Revit starts.
        /// </summary>
        Result OnStartup(IContainer container, UIControlledApplication application);
    }
}