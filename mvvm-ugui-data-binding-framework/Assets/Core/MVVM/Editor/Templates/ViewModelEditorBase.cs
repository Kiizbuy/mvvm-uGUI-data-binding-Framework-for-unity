using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MVVM.Runtime.Binders;
using MVVM.Runtime.Core;
using UnityEditor.UIElements;

namespace MVVM.Editor.Binders
{
    public abstract class ViewModelEditorBase : UnityEditor.Editor
    {
        protected static List<Type> ViewModelTypes;
        protected static List<string> ViewModelTypeNames;

        protected static readonly Color ErrorColor = new Color(1f, 0.4f, 0.4f);

        private static void EnsureViewModelTypesLoaded()
        {
            if (ViewModelTypes != null)
                return;

            var pocoBaseType = typeof(ViewModel);

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .ToArray();

            ViewModelTypes = allTypes
                .Where(t => pocoBaseType.IsAssignableFrom(t))
                .ToList();

            ViewModelTypeNames = ViewModelTypes.Select(t => t.FullName).ToList();
        }

        protected PopupField<string> CreateViewModelTypePopup(SerializedProperty prop, string label = "ViewModel Type")
        {
            EnsureViewModelTypesLoaded();
            SetSingleViewModelTypeIfNeeded(prop);

            var currentValue = prop.stringValue;
            var selectedIndex = Mathf.Max(0, ViewModelTypeNames.IndexOf(currentValue));

            if (selectedIndex == -1 && !string.IsNullOrEmpty(currentValue))
            {
                Debug.LogError(GetInvalidViewModelTypeMessage(currentValue));
            }

            var popup = new PopupField<string>(
                label,
                ViewModelTypeNames,
                selectedIndex,
                s => s.Split('.').Last(),
                s => s.Split('.').Last()
            );

            if (selectedIndex == -1 && !string.IsNullOrEmpty(currentValue))
            {
                var labelElement = popup.Q<Label>();
                if (labelElement != null)
                {
                    labelElement.style.color = ErrorColor;
                }
            }

            popup.RegisterValueChangedCallback(evt =>
            {
                var newIndex = ViewModelTypeNames.IndexOf(evt.newValue);
                if (newIndex >= 0)
                {
                    prop.stringValue = ViewModelTypeNames[newIndex];
                    serializedObject.ApplyModifiedProperties();
                    
                    var labelElement = popup.Q<Label>();
                    if (labelElement != null)
                    {
                        labelElement.style.color = StyleKeyword.Null;
                    }
                }
            });

            popup.BindProperty(prop);

            return popup;
        }

        protected virtual string GetInvalidViewModelTypeMessage(string invalidTypeName)
        {
            return $"ViewModel type '{invalidTypeName}' not found. " +
                   $"Available types: {string.Join(", ", ViewModelTypeNames.Select(t => t.Split('.').Last()))}";
        }

        private void SetSingleViewModelTypeIfNeeded(SerializedProperty prop)
        {
            if (ViewModelTypeNames == null)
                return;
            
            if (ViewModelTypeNames.Count == 1 && string.IsNullOrEmpty(prop.stringValue))
            {
                prop.stringValue = ViewModelTypeNames[0];
                serializedObject.ApplyModifiedProperties();
            }
        }

        protected void CollectRootCandidates(out List<MonoBehaviour> rootCandidates,
            out List<string> rootCandidateNames)
        {
            rootCandidates = new List<MonoBehaviour>();
            rootCandidateNames = new List<string>();

            var comp = target as Component;
            if (comp == null) return;

            var monos = comp.GetComponentsInParent<MonoBehaviour>(true);
            
            foreach (var m in monos)
            {
                if (m == null) 
                    continue;
                if (m is not IViewModelProvider) 
                    continue;
                
                rootCandidates.Add(m);
                rootCandidateNames.Add(m.GetType().Name);
            }
        }

        protected void GetBindableMembersFromProvider(MonoBehaviour provider,
            out string[] memberNames,
            out Dictionary<string, Type> memberTypes)
        {
            memberTypes = new Dictionary<string, Type>();
            memberNames = Array.Empty<string>();
            if (provider == null) return;

            Type vmType = null;
            if (provider is IViewModelProvider prov)
            {
                try
                {
                    vmType = prov.GetViewModel()?.GetType();
                }
                catch
                {
                    /* ignored */
                }
            }

            if (vmType == null)
                return;

            var props = vmType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<BindableAttribute>() != null)
                .Where(p => typeof(ViewModel).IsAssignableFrom(p.PropertyType));

            foreach (var p in props)
                memberTypes[p.Name] = p.PropertyType;
            
            memberNames = memberTypes.Keys.OrderBy(n => n).ToArray();
        }

        protected PopupField<string> CreateMemberPopup(SerializedProperty memberNameProp, 
                                                     string[] availableMembers, 
                                                     string label = "Member",
                                                     MonoBehaviour provider = null)
        {
            var currentMemberName = memberNameProp.stringValue;
            var idx = Array.IndexOf(availableMembers, currentMemberName);
            
            if (idx == -1 && !string.IsNullOrEmpty(currentMemberName))
            {
                Debug.LogError(GetInvalidMemberMessage(provider, currentMemberName, availableMembers));
            }

            var memberPopup = new PopupField<string>(
                label, 
                availableMembers.ToList(), 
                Mathf.Max(0, idx));
            
            memberPopup.BindProperty(memberNameProp);

            if (idx == -1 && !string.IsNullOrEmpty(currentMemberName))
            {
                var labelElement = memberPopup.Q<Label>();
                if (labelElement != null)
                {
                    labelElement.style.color = ErrorColor;
                }
            }

            memberPopup.RegisterValueChangedCallback(evt =>
            {
                var newIdx = Array.IndexOf(availableMembers, evt.newValue);
                if (newIdx >= 0)
                {
                    memberNameProp.stringValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                    
                    var labelElement = memberPopup.Q<Label>();
                    if (labelElement != null)
                    {
                        labelElement.style.color = StyleKeyword.Null;
                    }
                }
            });

            return memberPopup;
        }

        protected virtual string GetInvalidMemberMessage(MonoBehaviour provider, string invalidMemberName, string[] availableMembers)
        {
            var providerName = provider != null ? provider.GetType().Name : "Unknown";
            return $"Member '{invalidMemberName}' not found in provider '{providerName}'. " +
                   $"Available members: {string.Join(", ", availableMembers)}";
        }
    }
}