﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DSkin.DirectUI;
using JavaScript.Manager;
using JavaScript.Manager.Loaders;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace Tabris.Winform
{
    public partial class TabrisWinform : DSkin.Forms.DSkinForm
    {
        private WinformLogExcutor logExcutor;
        private RuntimeManager manager;
        public TabrisWinform()
        {
            InitializeComponent();

            var domanPath = AppDomain.CurrentDomain.BaseDirectory;
            var tabrisFolder = Path.Combine(domanPath, "tabris");
            if (!Directory.Exists(tabrisFolder))
            {
                throw new FileNotFoundException(tabrisFolder);
            }
            var indexFile = Path.Combine(tabrisFolder, "tabris.html");
            if (!File.Exists(indexFile))
            {
                throw new FileNotFoundException(indexFile);
            }
            this.codemirrow.Url = "file:///" + indexFile;

            logExcutor = new WinformLogExcutor(this.Log);



            manager = new RuntimeManager(new ManualManagerSettings { ScriptTimeoutMilliSeconds = 0 });

            JavaScript.Manager.Tabris.Tabris.Register(new JavaScript.Manager.Tabris.TabrisOptions
            {
                LogExecutor = logExcutor
            });

        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExcutor_Click(object sender, EventArgs e)
        {
            Log(LogLevel.INFO, "DASaaaaaaaaaaaaaa");
            //var code = this.codemirrow.InvokeJS("getCode()").ToString();
            //if (string.IsNullOrEmpty(code))
            //{
            //    MessageBox.Show("执行内容为空");
            //    return;
            //}

            //invokeJsCode(code);
        }

        private void btExcutorSelected_Click(object sender, EventArgs e)
        {
            var selectedCode = this.codemirrow.InvokeJS("getSelectedCode()").ToString();
            if (string.IsNullOrEmpty(selectedCode))
            {
                MessageBox.Show("获取选择内容为空");
                return;
            }
            invokeJsCode(selectedCode);
        }

        private void invokeJsCode(string code)
        {
            Enable(false);
            Task.Factory.StartNew( async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(code))
                    {
                        MessageBox.Show("获取选择内容为空");
                        return;
                    }
                    if (this.catchBox.CheckState.Equals(CheckState.Checked))
                    {
                        code = "var tabris = require('javascript_tabris');\n" + "try{\n" + code +
                               "\n}catch(err){host.err=err.message}";
                    }
                    else
                    {
                        code = "var tabris = require('javascript_tabris');\n" + code;
                    }
                    dynamic host = new ExpandoObject();
                    var option = new ExecutionOptions
                    {
                        HostObjects = new List<HostObject> {new HostObject {Name = "host", Target = host}}
                    };

                    await manager.ExecuteAsync(Guid.NewGuid().ToString(), code, option);
                    try
                    {
                        if (!string.IsNullOrEmpty(host.err.ToString()))
                        {
                            Log(LogLevel.ERROR, host.err);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    await Task.Delay(5000);
                }
                catch (ScriptEngineException ex)
                {
                    Log(LogLevel.ERROR, ((Microsoft.ClearScript.ScriptEngineException) ex).ErrorDetails);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.ERROR, ex.Message);
                }
                finally
                {
                    Enable(true);
                }


            }).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    while (ex is AggregateException && ex.InnerException != null)
                        ex = ex.InnerException;
                    Log(LogLevel.ERROR,ex.Message);
                }
                else if (t.IsCanceled)
                {
                    Log(LogLevel.WARN,"Canclled.");
                }
            });
            

        }

        private void Enable(bool flag)
        {
            this.Invoke(new EventHandler(delegate
            {
                btnExcutor.Enabled = flag;
                btExcutorSelected.Enabled = flag;
                reloadRuntime.Enabled = flag;
                catchBox.Enabled = flag;
            }));
        }
        private void Log(LogLevel level, string msg, string trace = null)
        {
            msg = msg + trace;
            this.Invoke(new EventHandler(delegate
            {
                var levelStr = GetDescription(level);
                if (level.Equals(LogLevel.ERROR))
                {
                    logList.Items.Add(new DuiHtmlLabel
                    {
                        Text = string.Format("&nbsp;&nbsp; <label color='red'>[{0:yyyy-MM-dd HH:mm:ss} {1}]--------{2} </label>", DateTime.Now, levelStr, msg),
                        AutoSize = true
                    });
                }
                else if (level.Equals(LogLevel.WARN))
                {
                    logList.Items.Add(new DuiHtmlLabel
                    {
                        Text = string.Format("&nbsp;&nbsp; <label color='blue'>[{0:yyyy-MM-dd HH:mm:ss} {1}]--------{2} </label>", DateTime.Now, levelStr, msg),
                        AutoSize = true
                    });
                }
                else
                {
                    logList.Items.Add(new DuiHtmlLabel
                    {
                        Text = string.Format("&nbsp;&nbsp; [{0:yyyy-MM-dd HH:mm:ss} {1}]--------{2}", DateTime.Now, levelStr, msg),
                        AutoSize = true
                    });
                }

                logList.Value = 1;

            }));

        }

        private string GetDescription(System.Enum value, Boolean nameInstead = true)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name == null)
            {
                return null;
            }
            FieldInfo field = type.GetField(name);
            DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (attribute == null && nameInstead == true)
            {
                return name;
            }
            return attribute == null ? null : attribute.Description;
        }

        private void reloadRuntime_Click(object sender, EventArgs e)
        {
            try
            {
                manager.Dispose();
                manager = new RuntimeManager(new ManualManagerSettings { ScriptTimeoutMilliSeconds = 0 });
                RequireManager.ClearPackages();
                JavaScript.Manager.Tabris.Tabris.Register(new JavaScript.Manager.Tabris.TabrisOptions
                {
                    LogExecutor = logExcutor
                });

                Log(LogLevel.INFO, "重新加载运行时成功");
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "重新加载运行时失败" + ex.Message);
            }
        }
    }


}
