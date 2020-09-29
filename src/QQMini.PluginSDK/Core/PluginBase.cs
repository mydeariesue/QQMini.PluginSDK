﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using QQMini.PluginFramework.Utility.Core;
using QQMini.PluginSDK.Core.Model;

namespace QQMini.PluginSDK.Core
{
	/// <summary>
	/// 提供一种符合 QQMini 扩展应用程序的运行机制
	/// </summary>
	[Serializable]
	public abstract class PluginBase : MarshalByRefObject, IPlugin
	{
		#region --字段--
		private QMApi _qMApi;
		private bool _isInitialized;

		private static readonly MethodInfo[] _methodInfo;
		#endregion

		#region --属性--
		/// <summary>
		/// 获取当前插件可使用的 QQMini 框架的 <see cref="PluginSDK.Core.QMApi"/> 实例
		/// </summary>
		public QMApi QMApi { get => this._qMApi; }
		/// <summary>
		/// 获取当前插件是否已经初始化
		/// </summary>
		public bool IsInitialized { get => this._isInitialized; }
		/// <summary>
		/// 当在派生类中重写时, 设置应用程序的信息
		/// </summary>
		public abstract PluginInfo PluginInfo { get; }
		#endregion

		#region --构造函数--
		static PluginBase ()
		{
			_methodInfo = typeof (PluginBase).GetMethods ().Where (p => p.GetCustomAttribute<QMEventAttribute> () != null).ToArray ();
		}
		#endregion

		#region --公开方法--
		/// <summary>
		/// 获取插件的基本信息
		/// </summary>
		/// <returns>插件基本信息的字符串</returns>
		string IPlugin.GetInfomaction ()
		{
			return this.PluginInfo.ToString ();
		}
		/// <summary>
		/// 设置插件的授权信息
		/// </summary>
		/// <param name="authCode">插件授权码</param>
		void IPlugin.SetAuthorize (int authCode)
		{
			this._qMApi = new QMApi (authCode);
		}
		/// <summary>
		/// 设置插件初始化
		/// </summary>
		void IPlugin.SetInitialize ()
		{
			this.OnInitialize ();
			this._isInitialized = true;
		}
		/// <summary>
		/// 设置插件反初始化
		/// </summary>
		void IPlugin.SetUninitialize ()
		{
			this._isInitialized = false;
			this.OnUninitialize ();
		}
		/// <summary>
		/// 设置插件打开设置菜单
		/// </summary>
		void IPlugin.SetOpenSettingMenu ()
		{
			this.OnOpenSettingMenu ();
		}
		/// <summary>
		/// 向当前插件推送新事件
		/// </summary>
		/// <param name="type">事件类型</param>
		/// <param name="subType">事件子类型</param>
		/// <param name="datas">数据指针数组</param>
		/// <returns>事件的处理结果</returns>
		QMEventHandlerTypes IPlugin.PushNewEvent (int type, int subType, params IntPtr[] datas)
		{
			foreach (MethodInfo method in _methodInfo)
			{
				QMEventAttribute attribute = method.GetCustomAttribute<QMEventAttribute> ();    // 获取方法标记

				if ((int)attribute.Type == type && attribute.SubType == subType)
				{
					// 获取方法的第一个参数
					ParameterInfo methodParameter = method.GetParameters ().SingleOrDefault ();
					if (methodParameter != null)
					{
						// 获取参数的构造函数的唯一构造函数
						ConstructorInfo constructorInfo = methodParameter.ParameterType.GetConstructors ().SingleOrDefault ();

						if (constructorInfo != null)
						{
							// 根据参数获取数据类型
							ParameterInfo[] parameters = constructorInfo.GetParameters ();
							object[] args = new object[parameters.Length];
							args[0] = type;
							args[1] = subType;
							for (int i = 0; i < parameters.Length - 2; i++)
							{
								// 将指针转换为具体类型的数据
								args[i + 2] = datas[i].GetValue (parameters[i + 2].ParameterType);  // 转换为
							}

							// 创建事件参数
							QMEventArgs qMEventArgs = (QMEventArgs)Activator.CreateInstance (methodParameter.ParameterType, args);

							// 调用总事件
							QMEventHandlerTypes result = OnReceiveEvent (qMEventArgs);
							if (result != QMEventHandlerTypes.Continue)
							{
								return result;
							}

							// 调用具体路由方法
							return (QMEventHandlerTypes)method.Invoke (this, new object[] { qMEventArgs });
						}
					}
				}
			}

			return QMEventHandlerTypes.Continue;
		}

		/// <summary>
		/// 当插件被初始化时调用
		/// </summary>
		public virtual void OnInitialize ()
		{ }
		/// <summary>
		/// 当插件被反初始化时调用
		/// </summary>
		public virtual void OnUninitialize ()
		{ }
		/// <summary>
		/// 当插件打开设置菜单时调用
		/// </summary>
		public virtual void OnOpenSettingMenu ()
		{ }

		/// <summary>
		/// 当插件收到新事件时调用
		/// </summary>
		/// <param name="e">新事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		public virtual QMEventHandlerTypes OnReceiveEvent (QMEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到好友消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.PrivateMessage, SubType = (int)QMPrivateEventSubTypes.Friend)]
		public virtual QMEventHandlerTypes OnReceiveFriendMessage (QMPrivateMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到群组临时消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.PrivateMessage, SubType = (int)QMPrivateEventSubTypes.GroupTemp)]
		public virtual QMEventHandlerTypes OnReceiveGroupTempMessage (QMPrivateMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到讨论组临时消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.PrivateMessage, SubType = (int)QMPrivateEventSubTypes.DiscussTemp)]
		public virtual QMEventHandlerTypes OnReceiveDiscussTempMessage (QMPrivateMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到在线临时消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.PrivateMessage, SubType = (int)QMPrivateEventSubTypes.OnlineTemp)]
		public virtual QMEventHandlerTypes OnReceiveOnlineTempMessage (QMPrivateMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到好友验证回复消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.PrivateMessage, SubType = (int)QMPrivateEventSubTypes.FriendVerify)]
		public virtual QMEventHandlerTypes OnReceiveFriendVerifyMessage (QMPrivateMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到群组消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMessage, SubType = (int)QMGroupEventSubTypes.Group)]
		public virtual QMEventHandlerTypes OnReceiveGroupMessage (QMGroupMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当收到讨论组消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.DiscussMessage, SubType = (int)QMDiscussEventSubTypes.Discuss)]
		public virtual QMEventHandlerTypes OnReceiveDiscussMessage (QMDiscussMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当好友添加请求
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.FriendAddRequest, SubType = (int)QMFriendAddRequestEventSubTypes.FriendAddRequest)]
		public virtual QMEventHandlerTypes OnFriendAddRequest (QMFriendAddRequestEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当被同意添加为好友
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.FriendAddResponse, SubType = (int)QMFriendAddResponseEventSubTypes.AgreeAddFriend)]
		public virtual QMEventHandlerTypes OnBeAgreeAddFriend (QMFriendAddResponseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当被拒绝添加为好友
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.FriendAddResponse, SubType = (int)QMFriendAddResponseEventSubTypes.RefuseAddFriend)]
		public virtual QMEventHandlerTypes OnBeRefuseAddFriend (QMFriendAddResponseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当被删除好友
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.BeFriendDelete, SubType = (int)QMBeFriendDeleteEventSubTypes.BeFriendDelete)]
		public virtual QMEventHandlerTypes OnBeFriendDelete (QMBeFriendDeleteEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组申请加入请求
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupAddRequest, SubType = (int)QMGroupAddRequestEventSubTypes.ApplyAddGroup)]
		public virtual QMEventHandlerTypes OnGroupApplyAddRequest (QMGroupAddRequestEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组邀请我加入请求
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupAddRequest, SubType = (int)QMGroupAddRequestEventSubTypes.InviteMyAddGroup)]
		public virtual QMEventHandlerTypes OnGroupInviteMyAddRequest (QMGroupAddRequestEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组成员被允许入群
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberInstance, SubType = (int)QMGroupMemberIncreaseEventSubTypes.BeAllowAddGroup)]
		public virtual QMEventHandlerTypes OnGroupMemberBeAllowAdd (QMGroupMemberIncreaseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当我加入了群组
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberInstance, SubType = (int)QMGroupMemberIncreaseEventSubTypes.MyAddGroup)]
		public virtual QMEventHandlerTypes OnGroupMemberMyAddGroup (QMGroupMemberIncreaseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组成员被邀请入群
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberInstance, SubType = (int)QMGroupMemberIncreaseEventSubTypes.BeInviteAddGroup)]
		public virtual QMEventHandlerTypes OnGroupMemberBeInviteAdd (QMGroupMemberIncreaseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组成员离开
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberDecrease, SubType = (int)QMGroupMemberDecreaseEventSubTypes.GroupMemberLeave)]
		public virtual QMEventHandlerTypes OnGroupMemberLeave (QMGroupMemberIncreaseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组管理员移除成员
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberDecrease, SubType = (int)QMGroupMemberDecreaseEventSubTypes.GroupManagerRemoveMember)]
		public virtual QMEventHandlerTypes OnGroupManagerRemoveMember (QMGroupMemberIncreaseEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组解散
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupDissmiss, SubType = (int)QMGroupDissmissEventSubTypes.GroupDissmiss)]
		public virtual QMEventHandlerTypes OnGroupDissmiss (QMGroupDissmissEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群成员成为管理员
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupManagerChange, SubType = (int)QMGroupManagerChangeEventSubTypes.BecomeManager)]
		public virtual QMEventHandlerTypes OnGroupMemberBecomeManager (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群成员被取消管理员
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupManagerChange, SubType = (int)QMGroupManagerChangeEventSubTypes.CanceledManager)]
		public virtual QMEventHandlerTypes OnGroupMemberCanceledManager (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群成员修改了新名片
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberCardChange, SubType = (int)QMGroupMemberCardChangeEventSubTypes.GroupMemberCardChange)]
		public virtual QMEventHandlerTypes OnGroupMemberCardChange (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组名称改变
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupNameChange, SubType = (int)QMGroupNameChangeEventSubTypes.GroupNameChange)]
		public virtual QMEventHandlerTypes OnGroupNameChange (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组开启全体禁言
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupBanSpeak, SubType = (int)QMGroupBanSpeakEventSubTypes.GroupBanSpeakOpen)]
		public virtual QMEventHandlerTypes OnGroupBanSpeakOpen (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组关闭全体禁言
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupBanSpeak, SubType = (int)QMGroupBanSpeakEventSubTypes.GroupBanSpeakClose)]
		public virtual QMEventHandlerTypes OnGroupBanSpeakClose (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组成员被设置禁言
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberBanSpeak, SubType = (int)QMGroupMemberBanSpeakEventSubTypes.GroupMemberSetBanSpeak)]
		public virtual QMEventHandlerTypes OnGroupMemberSetBanSpeak (QMGroupMemberBanSpeakEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组成员被解除禁言
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberBanSpeak, SubType = (int)QMGroupMemberBanSpeakEventSubTypes.GroupMemberRemoveBanSpeak)]
		public virtual QMEventHandlerTypes OnGroupMemberRemoveBanSpeak (QMGroupMemberBanSpeakEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组匿名被开启
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupAnonymousChange, SubType = (int)QMGroupAnonymousChangeEventSubTypes.GroupAnonymousOpen)]
		public virtual QMEventHandlerTypes OnGroupAnonymousOpen (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组匿名被关闭
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupAnonymousChange, SubType = (int)QMGroupAnonymousChangeEventSubTypes.GroupAnonymousClose)]
		public virtual QMEventHandlerTypes OnGroupAnonymousClose (QMGroupManagerChangeEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		/// <summary>
		/// 当群组成员撤回消息
		/// </summary>
		/// <param name="e">包含当前事件的事件参数</param>
		/// <returns>通知当前框架的事件处理办法</returns>
		[QMEvent (QMEventTypes.GroupMemberRemoveMessage, SubType = (int)QMGroupMemberRemoveMessageEventSubTypes.RemoveGroupMessage)]
		public virtual QMEventHandlerTypes OnGroupMemberRemoveMessage (QMGroupMemberRemoveMessageEventArgs e)
		{
			return QMEventHandlerTypes.Continue;
		}
		#endregion
	}
}
