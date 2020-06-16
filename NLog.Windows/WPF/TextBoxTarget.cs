using NLog.Common;
using NLog.Targets;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NLog.Windows.WPF
{
    [Target("TextBox")]
    public sealed class TextBoxTarget : TargetWithLayout
    {
        /// <summary>
        /// 用于显示日志的控件
        /// </summary>
        public TextBox TargetTextBox { get; set; }

        /// <summary>
        /// 显示日志的文本框的标签
        /// </summary>
        public string TextBoxTag { get; set; }

        /// <summary>
        /// 承载显示日志文本框的窗口的标题
        /// </summary>
        public string WindowTitle { get; set; }


        private int m_MaxLines = 50;
        /// <summary>
        /// 显示控件中最多存储多少行日志，最小50行，最大500行。默认50行
        /// </summary>
        public int MaxLines
        {
            get => this.m_MaxLines;
            set
            {
                if (value < 50)  value = 50;
                if (value > 500) value = 500;

                this.m_MaxLines = value;
            }
        }

        public static void ReInitializeTarget(System.Windows.Window window)
        {
            InternalLogger.Info("Executing ReInitializeTarget for Window {0}", window);
            var targets = LogManager.Configuration.AllTargets;
            foreach (var target in LogManager.Configuration.AllTargets)
            {
                var textBoxTarget = target as TextBoxTarget;

                if (textBoxTarget == null || textBoxTarget.WindowTitle != window.Title) 
                    continue;

                var textBox = GetChildByTag<TextBox>(window, textBoxTarget.TextBoxTag);
                if (textBox == null) 
                    continue;
                
                if ( textBoxTarget.TargetTextBox == null || textBoxTarget.TargetTextBox != textBox)
                {
                    textBoxTarget.AttachToControl(window, textBox);
                }
            }
        }

        private void AttachToControl(Window window, TextBox textBox)
        {
            InternalLogger.Info("Attaching target {0} to textbox {1}.{2}", this.Name, window.Title, textBox.Name);
            this.TargetTextBox = textBox;
            this.TargetTextBox.IsReadOnly = true;
            this.TargetTextBox.MouseDoubleClick += (sender, args) => this.TargetTextBox.Clear();
        }


        /// <summary>
        /// 初始化Target，NLog系统内部调用
        /// </summary>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            if (this.TargetTextBox != null)
                return;

            if (string.IsNullOrEmpty(WindowTitle) || string.IsNullOrWhiteSpace(WindowTitle))
            {
                InternalLogger.Info("The param of WindowTag is set to null, empty or whitespace, please correct it.");
                return;
            }

            if (string.IsNullOrEmpty(TextBoxTag) || string.IsNullOrWhiteSpace(TextBoxTag))
            {
                InternalLogger.Info("The param of TextBoxTag is set to null, empty or whitespace, please correct it.");
                return;
            }

            // 根据窗口名查找窗口
            if(Application.Current==null)
                return;

            var parentWindowOfTextBox = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window.Title == WindowTitle);
            if (parentWindowOfTextBox == null)
            {
                InternalLogger.Error($"Can not find the window that is named {WindowTitle}, waiting for ReInitializeTarget.");
                return;
            }

            // 根据控件Tag在指定的窗口中查找控件
            var textBox = GetChildByTag<TextBox>(parentWindowOfTextBox, TextBoxTag);
            if (textBox == null)
            {
                InternalLogger.Error($"Can not find the TextBox that is named {TextBoxTag}, waiting for ReInitializeTarget.");
                return;
            }

            AttachToControl(parentWindowOfTextBox, textBox);
        }

        /// <summary>
        /// 日志输出
        /// </summary>
        /// <param name="logEvent"></param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (this.TargetTextBox == null)
                return;

            // 避免产生跨线程调用异常
            var dispatcher = this.TargetTextBox.Dispatcher;
            dispatcher?.BeginInvoke(new Action<string>(SendTheMessageToTextBox), Layout.Render(logEvent));
        }

        /// <summary>
        /// 在TextBox中显示日志
        /// </summary>
        /// <param name="logMessage">日志消息</param>
        private void SendTheMessageToTextBox(string logMessage)
        {
            if(this.TargetTextBox == null)
                return;

            try
            {
                if (this.TargetTextBox.LineCount > this.m_MaxLines)
                    this.TargetTextBox.Clear();

                this.TargetTextBox.AppendText(logMessage);
                this.TargetTextBox.ScrollToEnd();
            }
            catch (Exception ex)
            {
                InternalLogger.Warn(ex.ToString());
            }
        }

        /// <summary>
        /// 根据元素的Tag信息查找指定元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="elementTag"></param>
        /// <returns></returns>
        private static T GetChildByTag<T>(DependencyObject obj, string elementTag) where T : FrameworkElement
        {
            foreach(DependencyObject child in LogicalTreeHelper.GetChildren(obj))
            {
                if (child is T element && (element.Tag.ToString() == elementTag))
                    return element;

                var grandChild = GetChildByTag<T>(child, elementTag);
                if (grandChild != null)
                {
                    return grandChild;
                }
            }

            return null;
        }
    }
}