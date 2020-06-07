﻿using Onbox.Di.V7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Onbox.Mvc.V7
{
    public interface INavigator
    {
        INavigatorSubscription Attach<TParent, TNavComponent>(TParent parentComponent, TNavComponent component)
            where TParent : IMvcLifecycleComponent
            where TNavComponent : NavigatorComponent;
        MvcComponentBase GetCurrentPage<TParent>(string componentName = "Navigator") 
            where TParent : IMvcLifecycleComponent;
        void ClearNavigation<TParent>(string componentName = "Navigator") 
            where TParent : IMvcLifecycleComponent;
        void Navigate<TParent, TComponent>(string componentName = "Navigator")
            where TParent : IMvcLifecycleComponent
            where TComponent : IMvcComponent;
        INavigatorSubscription Subscribe<TParent>(Action<IMvcComponent> action) 
            where TParent : IMvcLifecycleComponent;
        INavigatorSubscription Subscribe<TParent>(string componentName, Action<IMvcComponent> action) 
            where TParent : IMvcLifecycleComponent;
    }

    public class Navigator : INavigator
    {
        public readonly Dictionary<string, Dictionary<string, Type>> componentDictionary = new Dictionary<string, Dictionary<string, Type>>();
        public readonly Dictionary<string, Dictionary<string, List<Action<IMvcComponent>>>> actionDictionary = new Dictionary<string, Dictionary<string, List<Action<IMvcComponent>>>>();
        private readonly IContainerResolver container;

        public Navigator(IContainerResolver container)
        {
            this.container = container;
        }

        public INavigatorSubscription Attach<TParent, TNavComponent>(TParent parentComponent, TNavComponent component) where TParent : IMvcLifecycleComponent where TNavComponent : NavigatorComponent
        {
            if (parentComponent == null)
            {
                throw new ArgumentException("Can not attach Navigator into a null View");
            }

            if (component == null)
            {
                throw new ArgumentException("Can not attach Navigator into a null Navigator Component");
            }

            if (string.IsNullOrWhiteSpace(component.Name))
            {
                throw new ArgumentException("Can not attach Navigator into Navigator Component with no name, please set x:Name on Xaml of your Navigator Component");
            }

            component.Loaded += this.Component_Loaded;
            component.Unloaded += this.Component_Unloaded;

            var parentIdentifier = GetParentIdentifier(parentComponent);

            if (componentDictionary.ContainsKey(parentIdentifier))
            {
                var navigators = componentDictionary[parentIdentifier];
                if (navigators == null)
                {
                    navigators = new Dictionary<string, Type>();
                }
            }
            else
            {
                componentDictionary[parentIdentifier] = new Dictionary<string, Type>();
            }

            var subs = Subscribe(parentIdentifier, component.Name, (page) => component.Content = page);
            component.NavigatorSubscription = subs;
            return subs;
        }

        private void Component_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NavigatorComponent navigatorComponent)
            {
                var parent = VisualTreeHelpers.GetParentMvcComponent(navigatorComponent);
                if (parent != null)
                {
                    var parentIdentifier = GetParentIdentifier(parent);
                    navigatorComponent.Content = GetCurrentPage(parentIdentifier, navigatorComponent.Name);
                }
            }
        }

        private void Component_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is NavigatorComponent navigatorComponent)
            {
                navigatorComponent.NavigatorSubscription?.UnSubscribe();
            }
        }

        private string GetParentIdentifier(IMvcLifecycleComponent parent)
        {
            var type = parent.GetType();
            return type.FullName;
        }

        private string GetParentIdentifier<TParent>() where TParent : IMvcLifecycleComponent
        {
            var type = typeof(TParent);
            var identitifier = type.FullName;
            return identitifier;
        }

        public MvcComponentBase GetCurrentPage<TParent>(string componentName = "Navigator") where TParent : IMvcLifecycleComponent
        {
            string identitifier = GetParentIdentifier<TParent>();
            return GetCurrentPage(identitifier, componentName);
        }

        private MvcComponentBase GetCurrentPage(string parentIdentifier, string componentName)
        {
            if (componentDictionary.ContainsKey(parentIdentifier))
            {
                var navigators = componentDictionary[parentIdentifier];
                if (navigators != null && navigators.ContainsKey(componentName))
                {
                    var pageType = navigators[componentName];
                    return InstantiateComponent(pageType);
                }
            }

            return null;
        }

        private MvcComponentBase InstantiateComponent(Type componentType)
        {
            if (componentType != null)
            {
                return this.container.Resolve(componentType) as MvcComponentBase;
            }
            else
            {
                return null;
            }
        }

        public void Navigate<TParent, TComponent>(string componentName = "Navigator") where TParent : IMvcLifecycleComponent where TComponent : IMvcComponent
        {
            var parentIdentifier = GetParentIdentifier<TParent>();
            var componentType = typeof(TComponent);
            Navigate(parentIdentifier, componentName, componentType);
        }

        public void ClearNavigation<TParent>(string componentName = "Navigator") where TParent : IMvcLifecycleComponent
        {
            var parentIdentifier = GetParentIdentifier<TParent>();
            Navigate(parentIdentifier, componentName, null);
        }

        private void Navigate(string parentIdentifier, string componentName, Type componentType)
        {
            if (!componentDictionary.ContainsKey(parentIdentifier))
            {
                componentDictionary[parentIdentifier] = new Dictionary<string, Type>();
            }

            var navigators = componentDictionary[parentIdentifier];
            if (navigators == null)
            {
                navigators = new Dictionary<string, Type>();
                componentDictionary[componentName] = navigators;
            }

            navigators[componentName] = componentType;
            NotifySubscribers(parentIdentifier, componentName, componentType);
        }

        private void NotifySubscribers(string parentIdentifier, string componentName, Type componentType)
        {
            if (actionDictionary.ContainsKey(parentIdentifier))
            {
                var navigatorsActions = actionDictionary[parentIdentifier];

                if (navigatorsActions == null)
                {
                    return;
                }

                if (navigatorsActions.ContainsKey(componentName))
                {
                    var actions = navigatorsActions[componentName];
                    if (actions != null)
                    {
                        foreach (var action in actions)
                        {
                            var page = InstantiateComponent(componentType);
                            action.Invoke(page);
                        }
                    }
                }
            }
        }

        public INavigatorSubscription Subscribe<TParent>(Action<IMvcComponent> action) where TParent : IMvcLifecycleComponent
        {
            var componentName = "Navigator";
            return Subscribe<TParent>(componentName, action);
        }

        public INavigatorSubscription Subscribe<TParent>(string componentName, Action<IMvcComponent> action) where TParent : IMvcLifecycleComponent
        {
            var parentIdentifier = GetParentIdentifier<TParent>();
            return Subscribe(parentIdentifier, componentName, action);
        }

        private INavigatorSubscription Subscribe(string parentIdentifier, string componentName, Action<IMvcComponent> action)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                componentName = "Navigator";
            }

            var parentNavigatorActions = new Dictionary<string, List<Action<IMvcComponent>>>();
            var navigatorActions = new List<Action<IMvcComponent>>();

            if (actionDictionary.ContainsKey(parentIdentifier))
            {
                parentNavigatorActions = actionDictionary[parentIdentifier];
            }
            else
            {
                actionDictionary[parentIdentifier] = parentNavigatorActions;
            }

            if (parentNavigatorActions.ContainsKey(componentName))
            {
                navigatorActions = parentNavigatorActions[componentName];
            }
            else
            {
                parentNavigatorActions[componentName] = navigatorActions;
            }

            navigatorActions.Add(action);
            var pageSubs = new NavigatorSubscription(action, navigatorActions);
            return pageSubs;
        }
    }

    public interface INavigatorSubscription
    {
        void UnSubscribe();
    }

    public class NavigatorSubscription : INavigatorSubscription
    {
        private Action<IMvcComponent> action;
        private List<Action<IMvcComponent>> actions;

        public NavigatorSubscription(Action<IMvcComponent> action, List<Action<IMvcComponent>> actions)
        {
            this.action = action;
            this.actions = actions;
        }

        public void UnSubscribe()
        {
            this.actions.Remove(action);
            this.action = null;
            if (!actions.Any())
            {
                actions = null;
            }
        }
    }
}
