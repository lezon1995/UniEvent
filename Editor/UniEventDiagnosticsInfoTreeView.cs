#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UniEvent.Editor
{
    public class UniEventDiagnosticsInfoTreeViewItem : TreeViewItem
    {
        static Regex removeHref = new Regex("<a href.+>(.+)</a>", RegexOptions.Compiled);

        public int Count { get; set; }
        public string Head { get; set; }
        public TimeSpan Elapsed { get; set; }
        public IEnumerable<StackTraceInfo> StackTraces { get; set; }

        public UniEventDiagnosticsInfoTreeViewItem(int id) : base(id)
        {
        }
    }

    public class UniEventDiagnosticsInfoTreeView : TreeView
    {
        const string sortedColumnIndexStateKey = "UniEventDiagnosticsInfoTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItem> CurrentBindingItems;
        Dictionary<string, int> usedTrackIds = new Dictionary<string, int>();
        int trackId = -10000; // 0~ is used in StackTraceInfo

        public UniEventDiagnosticsInfoTreeView()
            : this(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Position") },
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Elapsed"), width = 5 },
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Count"), width = 5 },
            })))
        {
        }

        UniEventDiagnosticsInfoTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(sortedColumnIndexStateKey, 1);
        }

        public void ReloadAndSort()
        {
            var currentSelected = state.selectedIDs;
            Reload();
            Header_sortingChanged(multiColumnHeader);
            state.selectedIDs = currentSelected;
        }

        private void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(sortedColumnIndexStateKey, multiColumnHeader.sortedColumnIndex);
            var index = multiColumnHeader.sortedColumnIndex;
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var items = rootItem.children.Cast<UniEventDiagnosticsInfoTreeViewItem>();

            IOrderedEnumerable<UniEventDiagnosticsInfoTreeViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Head) : items.OrderByDescending(item => item.Head);
                    break;
                case 1:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Elapsed) : items.OrderByDescending(item => item.Elapsed);
                    break;
                case 2:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Count) : items.OrderByDescending(item => item.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { depth = -1 };

            var children = new List<TreeViewItem>();

            if (UniEventDiagnosticsInfoWindow.diagnosticsInfo != null)
            {
                var now = DateTimeOffset.UtcNow;
                if (UniEventDiagnosticsInfoWindow.EnableCollapse)
                {
                    var grouped = UniEventDiagnosticsInfoWindow.diagnosticsInfo.GetGroupedByCaller(false);
                    foreach (var item in grouped)
                    {
                        if (!usedTrackIds.TryGetValue(item.Key, out var id))
                        {
                            id = trackId++;
                            usedTrackIds[item.Key] = id;
                        }

                        var viewItem = new UniEventDiagnosticsInfoTreeViewItem(id)
                        {
                            Count = item.Count(),
                            Head = item.Key,
                            Elapsed = now - item.Last().Timestamp,
                            StackTraces = item
                        };
                        children.Add(viewItem);
                    }
                }
                else
                {
                    foreach (var item in UniEventDiagnosticsInfoWindow.diagnosticsInfo.GetCapturedStackTraces())
                    {
                        var viewItem = new UniEventDiagnosticsInfoTreeViewItem(item.Id)
                        {
                            Count = 1,
                            Head = item.Head,
                            Elapsed = now - item.Timestamp,
                            StackTraces = new[] { item }
                        };
                        children.Add(viewItem);
                    }
                }
            }

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItem>;
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as UniEventDiagnosticsInfoTreeViewItem;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect, item.Head, labelStyle);
                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.Elapsed.TotalSeconds.ToString(@"00.00"), labelStyle);
                        break;
                    case 2:
                        EditorGUI.LabelField(rect, item.Count.ToString(), labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
    }
}