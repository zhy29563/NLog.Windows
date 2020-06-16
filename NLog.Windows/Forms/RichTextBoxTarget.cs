using System;
using System.Linq;
using System.Windows.Forms;


namespace NLog.Windows.Forms
{
    using NLog;
    using Common;
    using Targets;
    
    [Target("RichTextBox")]
    public sealed class RichTextBoxTarget : TargetWithLayout
    {
        /// <summary>
        /// 用于显示日志的RichTextBox控件的名称
        /// </summary>
        public string TextBoxName { get; set; }

        /// <summary>
        /// 用于承载显示日志的RichTextBox控件的窗体名称
        /// </summary>
        public string FormName { get; set; }

        /// <summary>
        /// 在RichTextBox中默认显示日志的行数
        /// </summary>
        public int MaxLines { get; set; } = 50;

        /// <summary>
        /// 用于显示日志的RichTextBox控件
        /// </summary>
        public RichTextBox TargetRichTextBox { get; set; }
        
        /// <summary>
        /// 重新初始化RichTextBox目标类
        /// </summary>
        /// <param name="form">承载日志的窗体</param>
        public static void ReInitializeAllTextboxes(Form form)
        {
            InternalLogger.Info("Executing ReInitializeAllTextboxes for Form {0}", form);
            foreach (var target in LogManager.Configuration.AllTargets)
            {
                var textBoxTarget = target as RichTextBoxTarget;

                if (textBoxTarget == null || textBoxTarget.FormName != form.Name) 
                    continue;
                
                var richTextBox = FindControl<RichTextBox>(textBoxTarget.TextBoxName, form);
                if (richTextBox == null || richTextBox.IsDisposed) 
                    continue;
                
                if ( textBoxTarget.TargetRichTextBox == null || textBoxTarget.TargetRichTextBox.IsDisposed || textBoxTarget.TargetRichTextBox != richTextBox)
                {
                    textBoxTarget.AttachToControl(form, richTextBox);
                }
            }
        }

        /// <summary>
        /// 由NLog系统进行调用的重写函数
        /// </summary>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            
            if (TargetRichTextBox != null)
                return;

            if (string.IsNullOrEmpty(this.FormName))
            {
                InternalLogger.Error("FormName should be specified for {0}.{1}", GetType().Name, this.Name);
                return;
            }

            if (string.IsNullOrEmpty(this.TextBoxName))
            {
                InternalLogger.Error("Rich text box control name must be specified for {0}.{1}", GetType().Name, this.Name);
                return;
            }
            
            var openFormByName = Application.OpenForms[FormName];
            if (openFormByName == null)
            {
                InternalLogger.Info("Form {0} not found, waiting for ReInitializeAllTextboxes.", FormName);
                return;
            }

            var targetControl = FindControl<RichTextBox>(TextBoxName, openFormByName);
            if (targetControl == null)
            {
                InternalLogger.Info("Rich text box control '{0}' cannot be found on form '{1}'. Waiting for ReInitializeAllTextboxes.", TextBoxName, FormName);
                return;
            }

            AttachToControl(openFormByName, targetControl);
        }
        
        
        /// <summary>
        /// 绑定本地日志显示控件
        /// </summary>
        /// <param name="form">承载显示控件的窗体</param>
        /// <param name="textBox">显示控件</param>
        private void AttachToControl(Form form, RichTextBox textBox)
        {
            InternalLogger.Info("Attaching target {0} to textbox {1}.{2}", this.Name, form.Name, textBox.Name);
            this.TargetRichTextBox = textBox;
            this.TargetRichTextBox.ReadOnly = true;
            this.TargetRichTextBox.DoubleClick += (sender, args) => this.TargetRichTextBox.Clear();
        }
        
        /// <summary>
        /// 由NLog系统进行调用的写日志函数
        /// </summary>
        /// <param name="logEvent">日志事件信息</param>
        protected override void Write(LogEventInfo logEvent) => DispatchMessage(Layout.Render(logEvent));

        /// <summary>
        /// 使用异步的方式在RichTextBox上显示信息，以避免阻塞当前线程
        /// </summary>
        /// <param name="message"></param>
        private void DispatchMessage(string message)
        {
            var textBox = TargetRichTextBox;
            
            try
            {
                if (textBox == null || textBox.IsDisposed) 
                    return;
                
                if (textBox.InvokeRequired)
                    textBox.BeginInvoke(new Action<string>(this.DispatchMessage), message);
                else
                {
                    if (textBox.Lines.Length > this.MaxLines)
                        textBox.Clear();
                    
                    textBox.AppendText(message);
                    textBox.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Warn(ex.ToString());
            }
        }
        
        /// <summary>
        /// Finds control of specified type embended on searchControl.
        /// </summary>
        /// <typeparam name="TControl">The type of the control.</typeparam>
        /// <param name="name">Name of the control.</param>
        /// <param name="searchControl">Control in which we're searching for control.</param>
        /// <returns>
        /// A value of null if no control has been found.
        /// </returns>
        private static TControl FindControl<TControl>(string name, Control searchControl) where TControl : Control
        {
            if (searchControl.Name == name)
            {
                if (searchControl is TControl foundControl)
                {
                    return foundControl;
                }
            }

            return (from Control childControl in searchControl.Controls 
                select FindControl<TControl>(name, childControl)).FirstOrDefault(foundControl => foundControl != null);
        }
    }
}