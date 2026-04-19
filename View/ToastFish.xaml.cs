// ================================================================================
// 文件: View/ToastFish.xaml.cs
// 说明: 需要在 ToastFish.xaml.cs 中添加以下修改
// ================================================================================

// ------------------------------------------------------------
// 修改1: 在类成员变量区域添加（约第32行附近）
// ------------------------------------------------------------
        // 添加自定义词库测试的推送实例
        PushCustomizeWords pushCustomizeWords = new PushCustomizeWords();


// ------------------------------------------------------------
// 修改2: 在 MainWindow 构造函数的菜单初始化部分添加（约第245行附近）
// 在 "随机日语单词测试" 菜单项之后添加以下代码:
// ------------------------------------------------------------
            // ===== 自定义词库测试菜单 =====
            ToolStripItem RandomCustomWord = new ToolStripMenuItem("随机自定义词库测试");
            RandomCustomWord.Click += new EventHandler(RandomCustomWordTest_Click);
            ToolStripItem FlashCardTest = new ToolStripMenuItem("闪卡模式测试");
            FlashCardTest.Click += new EventHandler(FlashCardTest_Click);
            // ===== 添加结束 =====


// ------------------------------------------------------------
// 修改3: 在 RandomTest 下拉菜单中添加新菜单项（约第251行附近）
// 找到类似这样的代码:
//     ((ToolStripDropDownItem)contextMenuStrip1.Items[4]).DropDownItems.Add(RandomWord);
//     ((ToolStripDropDownItem)contextMenuStrip1.Items[4]).DropDownItems.Add(RandomGoin);
//     ((ToolStripDropDownItem)contextMenuStrip1.Items[4]).DropDownItems.Add(RandomJpWord);
// 在 RandomJpWord 之后添加:
// ------------------------------------------------------------
            // 添加自定义测试菜单项
            ((ToolStripDropDownItem)contextMenuStrip1.Items[4]).DropDownItems.Add(RandomCustomWord);
            ((ToolStripDropDownItem)contextMenuStrip1.Items[4]).DropDownItems.Add(FlashCardTest);


// ------------------------------------------------------------
// 修改4: 在类的末尾添加新的事件处理方法（约第660行附近，在 AutoPlay_Click 方法之后）
// ------------------------------------------------------------
        #region 自定义词库测试相关方法

        /// <summary>
        /// 随机自定义词库测试（四选一模式）
        /// </summary>
        private void RandomCustomWordTest_Click(object sender, EventArgs e)
        {
            // 检查是否有导入的自定义词库
            if (Select.CustomWordListForTest == null || Select.CustomWordListForTest.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("请先导入自定义词库！\n\n操作方法：右键托盘图标 -> 导入单词 -> 选择Excel文件", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var state = thread.ThreadState;
            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
            }
            
            // 设置当前表名为自定义
            Select.TABLE_NAME = "自定义";
            
            // 启动自定义词库测试线程
            thread = new Thread(new ParameterizedThreadStart(pushCustomizeWords.UnorderCustomWord));
            thread.Start(Select.WORD_NUMBER);
        }

        /// <summary>
        /// 闪卡模式测试（适用于问答题等非单词类型）
        /// </summary>
        private void FlashCardTest_Click(object sender, EventArgs e)
        {
            // 检查是否有导入的自定义词库
            if (Select.CustomWordListForTest == null || Select.CustomWordListForTest.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("请先导入自定义词库！\n\n操作方法：右键托盘图标 -> 导入单词 -> 选择Excel文件", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var state = thread.ThreadState;
            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
            }
            
            // 设置当前表名为自定义
            Select.TABLE_NAME = "自定义";
            
            // 启动闪卡测试线程
            thread = new Thread(new ParameterizedThreadStart(pushCustomizeWords.FlashCardTest));
            thread.Start(Select.WORD_NUMBER);
        }

        #endregion


// ------------------------------------------------------------
// 修改5: 在 ImportWords_Click 方法中，导入自定义词库后保存到静态列表
// 找到以下代码块（约第434-437行）:
//     else if (typeObj == typeCustWord)
//     {
//         Words.CustWordList = (List<CustomizeWord>)lstObj;
//         Select.TABLE_NAME = "自定义";
//     }
// 修改为:
// ------------------------------------------------------------
                else if (typeObj == typeCustWord)
                {
                    Words.CustWordList = (List<CustomizeWord>)lstObj;
                    Select.TABLE_NAME = "自定义";
                    // 保存自定义词库到静态列表，供测试使用
                    Select.SetCustomWordList((List<CustomizeWord>)lstObj);
                }
