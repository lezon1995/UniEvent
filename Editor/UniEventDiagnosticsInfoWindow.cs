#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniEvent.Editor
{
    public class UniEventDiagnosticsInfoWindow : EditorWindow
    {
        static readonly string Splitter = "---" + Environment.NewLine;

        static int interval;

        static UniEventDiagnosticsInfoWindow window;

        internal static DiagnosticsInfo diagnosticsInfo;


        [MenuItem("Window/UniEvent Diagnostics")]
        public static void OpenWindow()
        {
            if (window != null)
            {
                window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<UniEventDiagnosticsInfoWindow>("UniEvent Diagnostics").Show();
        }

        static readonly GUILayoutOption[] EmptyLayoutOption = new GUILayoutOption[0];

        UniEventDiagnosticsInfoTreeView treeView;
        object splitterState;

        void OnEnable()
        {
            window = this; // set singleton.
            splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            treeView = new UniEventDiagnosticsInfoTreeView();
            EnableAutoReload = EditorPrefs.GetBool("UniEventDiagnosticsInfoWindow.EnableAutoReload", false);
        }

        void OnGUI()
        {
            // Head
            RenderHeadPanel();

            // Splittable
            SplitterGUILayout.BeginVerticalSplit(splitterState, EmptyLayoutOption);
            {
                // Column Tabble
                RenderTable();

                // StackTrace details
                RenderDetailsPanel();
            }
            SplitterGUILayout.EndVerticalSplit();
        }

        #region HeadPanel

        static bool EnableAutoReload { get; set; }
        static bool EnableCaptureStackTrace { get; set; }
        internal static bool EnableCollapse { get; set; } = true;
        static readonly GUIContent EnableAutoReloadHeadContent = EditorGUIUtility.TrTextContent("Enable AutoReload", "Reload view automatically.", (Texture)null);
        static readonly GUIContent EnableCaptureStackTraceHeadContent = EditorGUIUtility.TrTextContent("Enable CaptureStackTrace", "CaptureStackTrace on Subscribe.", (Texture)null);
        static readonly GUIContent EnableCollapseHeadContent = EditorGUIUtility.TrTextContent("Collapse", "Collapse StackTraces.", (Texture)null);
        static readonly GUIContent ReloadHeadContent = EditorGUIUtility.TrTextContent("Reload", "Reload View.", (Texture)null);

        // [Enable CaptureStackTrace] | [Enable AutoReload] | .... | Reload
        void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(EmptyLayoutOption);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EmptyLayoutOption);

            // lazy initialize...
            if (diagnosticsInfo == null)
            {
                if (Events.IsInitialized)
                {
                    diagnosticsInfo = Events.DiagnosticsInfo;
                    EnableCaptureStackTrace = diagnosticsInfo.Options.EnableCaptureStackTrace;
                }
            }

            if (GUILayout.Toggle(EnableCaptureStackTrace, EnableCaptureStackTraceHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption) != EnableCaptureStackTrace)
            {
                if (CheckInitialized())
                {
                    diagnosticsInfo.Options.EnableCaptureStackTrace = EnableCaptureStackTrace = !EnableCaptureStackTrace;
                }
            }

            if (GUILayout.Toggle(EnableCollapse, EnableCollapseHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption) != EnableCollapse)
            {
                if (CheckInitialized())
                {
                    EnableCollapse = !EnableCollapse;
                    treeView.ReloadAndSort();
                    Repaint();
                }
            }

            if (GUILayout.Toggle(EnableAutoReload, EnableAutoReloadHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption) != EnableAutoReload)
            {
                if (CheckInitialized())
                {
                    EnableAutoReload = !EnableAutoReload;
                    EditorPrefs.SetBool("UniEventDiagnosticsInfoWindow.EnableAutoReload", EnableAutoReload);
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ReloadHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption))
            {
                if (CheckInitialized())
                {
                    treeView.ReloadAndSort();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        bool CheckInitialized()
        {
            if (diagnosticsInfo == null)
            {
                Debug.LogError("MessagePackDiagnosticsInfo is not set. Should call GlobalUniEvent.SetProvider on startup.");
                return false;
            }
            return true;
        }

        #endregion

        #region TableColumn

        Vector2 tableScroll;
        GUIStyle tableListStyle;

        void RenderTable()
        {
            if (tableListStyle == null)
            {
                tableListStyle = new GUIStyle("CN Box");
                tableListStyle.margin.top = 0;
                tableListStyle.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(tableListStyle, EmptyLayoutOption);

            tableScroll = EditorGUILayout.BeginScrollView(tableScroll, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(2000f)
            });
            var controlRect = EditorGUILayout.GetControlRect(new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            });


            treeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            if (diagnosticsInfo != null && EnableAutoReload)
            {
                if (interval++ % 120 == 0)
                {
                    if (diagnosticsInfo.CheckAndResetDirty())
                    {
                        treeView.ReloadAndSort();
                        Repaint();
                    }
                }
            }
        }

        #endregion

        #region Details

        static GUIStyle detailsStyle;
        Vector2 detailsScroll;

        void RenderDetailsPanel()
        {
            if (detailsStyle == null)
            {
                detailsStyle = new GUIStyle("CN Message");
                detailsStyle.wordWrap = false;
                detailsStyle.stretchHeight = true;
                detailsStyle.margin.right = 15;
            }

            string message = "";
            var selected = treeView.state.selectedIDs;
            if (selected.Count > 0)
            {
                var first = selected[0];
                var item = treeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) as UniEventDiagnosticsInfoTreeViewItem;
                if (item != null)
                {
                    var now = DateTimeOffset.UtcNow;
                    message = string.Join(Splitter, item.StackTraces
                        .Select(x =>
                            "Subscribe at " + x.Timestamp.ToLocalTime().ToString("HH:mm:ss.ff") // + ", Elapsed: " + (now - x.Timestamp).TotalSeconds.ToString("00.00")
                            + Environment.NewLine
                            + (x.formattedStackTrace ?? (x.formattedStackTrace = x.StackTrace.CleanupAsyncStackTrace()))));
                }
            }

            detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll, EmptyLayoutOption);
            var vector = detailsStyle.CalcSize(new GUIContent(message));
            EditorGUILayout.SelectableLabel(message, detailsStyle, new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(vector.x),
                GUILayout.MinHeight(vector.y)
            });
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}

