/* Yet Another Forum.net
 * Copyright (C) 2003 Bj�rnar Henden
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using YAF.Classes.Data;
using YAF.Classes.Utils;

namespace YAF.Pages
{
	/// <summary>
	/// Summary description for postmessage.
	/// </summary>
	public partial class deletemessage : YAF.Classes.Base.ForumPage
	{
		protected int OwnerUserId;
		protected DataRow msg;
		protected bool isModeratorChanged;

		public deletemessage()
			: base( "DELETEMESSAGE" )
		{

		}

		private void LinkedPosts_ItemDataBound( object sender, RepeaterItemEventArgs e )
		{
			if ( e.Item.ItemType == ListItemType.Header )
			{
				DeleteAllPosts = ( CheckBox ) e.Item.FindControl( "DeleteAllPosts" );
				DeleteAllPosts.Checked = DeleteAllPosts.Enabled = DeleteAllPosts.Checked = PageContext.ForumModeratorAccess || PageContext.IsAdmin;
				ViewState ["delAll"] = DeleteAllPosts.Checked;
			}
		}


		protected void Page_Load( object sender, System.EventArgs e )
		{
			msg = null;


			if ( Request.QueryString ["m"] != null )
			{
				using ( DataTable dt = YAF.Classes.Data.DB.message_list( Request.QueryString ["m"] ) )
					msg = dt.Rows [0];
				if ( !PageContext.ForumModeratorAccess && PageContext.PageUserID != ( int ) msg ["UserID"] )
					yaf_BuildLink.AccessDenied();
			}

			OwnerUserId = ( int ) msg ["UserID"];
			isModeratorChanged = ( PageContext.PageUserID != OwnerUserId );

			if ( PageContext.PageForumID == 0 )
				yaf_BuildLink.AccessDenied();
			if ( Request ["t"] == null && !PageContext.ForumPostAccess )
				yaf_BuildLink.AccessDenied();
			if ( Request ["t"] != null && !PageContext.ForumReplyAccess )
				yaf_BuildLink.AccessDenied();

			//Message.EnableRTE = PageContext.BoardSettings.AllowRichEdit;

			//Message.BaseDir = yaf_ForumInfo.ForumRoot + "editors";
			if ( !IsPostBack )
			{
				ViewState ["delAll"] = false;
				DeleteReasonRow.Visible = false;
				LinkedPosts.Visible = false;
				Cancel.Text = GetText( "Cancel" );
				PageLinks.AddLink( PageContext.BoardSettings.Name, yaf_BuildLink.GetLink( ForumPages.forum ) );
				PageLinks.AddLink( PageContext.PageCategoryName, yaf_BuildLink.GetLink( ForumPages.forum, "c={0}", PageContext.PageCategoryID ) );
				PageLinks.AddForumLinks( PageContext.PageForumID );
				if ( Request.QueryString ["m"] != null )
				{
					// delete message...
					PreviewRow.Visible = true;

					DataTable tempdb = YAF.Classes.Data.DB.message_getRepliesList( Request.QueryString ["m"] );

					if ( tempdb.Rows.Count != 0 )
					{
						LinkedPosts.Visible = true;
						LinkedPosts.DataSource = tempdb;
						LinkedPosts.DataBind();
					}
					string body = msg ["message"].ToString();
					/*	bool isHtml = body.IndexOf('<')>=0;
							if(isHtml) 
							{
									//throw new Exception("TODO: Convert this html message to forumcodes");
									body = FormatMsg.HtmlToForumCode(body);
							}
							//!!!*/
					if ( Request.QueryString ["action"].ToLower() == "delete" )
					{
						Title.Text = GetText( "EDIT" ); //GetText("EDIT");
						Delete.Text = GetText( "SAVE" ); // "GetText("Save");
					}
					else
					{
						Title.Text = GetText( "EDIT" );
						Delete.Text = GetText( "SAVE" ); // "GetText("Save");
					}

					Subject.InnerHtml = Server.HtmlDecode( Convert.ToString( msg ["Topic"] ) );
					DeleteReasonRow.Visible = true;
					ReasonEditor.Text = Server.HtmlDecode( Convert.ToString( msg ["DeleteReason"] ) );
					MessageFlags tFlags = new MessageFlags();
					//tFlags.IsHTML = Message.UsesHTML;
					//tFlags.IsBBCode = Message.UsesBBCode;
					string mbody = FormatMsg.FormatMessage( body, tFlags );
					PreviewCell.InnerHtml = mbody;

				}
			}
		}

		#region Web Form Designer generated code
		override protected void OnInit( EventArgs e )
		{
			// get the forum editor based on the settings
			//Message = yaf.editor.EditorHelper.CreateEditorFromType(PageContext.BoardSettings.ForumEditor);
			//	EditorLine.Controls.Add(Message);
			this.LinkedPosts.ItemDataBound += new System.Web.UI.WebControls.RepeaterItemEventHandler( this.LinkedPosts_ItemDataBound );

			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit( e );
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{

		}
		#endregion


		private bool PostLocked
		{
			get
			{
				if ( !PageContext.IsAdmin && PageContext.BoardSettings.LockPosts > 0 )
				{
					DateTime edited = ( DateTime ) msg ["Edited"];
					if ( edited.AddDays( PageContext.BoardSettings.LockPosts ) < DateTime.Now )
						return true;
				}
				return false;
			}
		}

		private bool PostDeleted
		{
			get
			{

				int deleted = ( int ) msg ["Flags"] & 8;
				if ( deleted == 8 )
					return true;
				return false;
			}
		}

		public bool CanDeletePost
		{
			get
			{
				return !PostLocked && ( ( int ) msg ["ForumFlags"] & ( int ) YAF.Classes.Data.ForumFlags.Locked ) != ( int ) YAF.Classes.Data.ForumFlags.Locked && ( ( int ) msg ["TopicFlags"] & ( int ) YAF.Classes.Data.TopicFlags.Locked ) != ( int ) YAF.Classes.Data.TopicFlags.Locked && ( ( int ) msg ["UserID"] == PageContext.PageUserID || PageContext.ForumModeratorAccess ) && PageContext.ForumDeleteAccess;
			}
		}

		public bool CanUnDeletePost
		{
			get
			{
				return PostDeleted && CanDeletePost;
			}
		}

		protected void Delete_Click( object sender, EventArgs e )
		{
			if ( !CanDeletePost )
				return;


			// CHANGED BAI 30.01.2004

			//Create objects for easy access
			object tmpMessageID = msg ["MessageID"];
			object tmpForumID = msg ["ForumID"];
			object tmpTopicID = msg ["TopicID"];

			// Delete message. If it is the last message of the topic, the topic is also deleted
			YAF.Classes.Data.DB.message_delete( tmpMessageID, isModeratorChanged, ReasonEditor.Text, PostDeleted ? 0 : 1, ( bool ) ViewState ["delAll"] );

			// retrieve topic information.
			DataRow topic = YAF.Classes.Data.DB.topic_info( tmpTopicID );

			//If topic has been deleted, redirect to topic list for active forum, else show remaining posts for topic
			if ( topic == null )
				yaf_BuildLink.Redirect( ForumPages.topics, "f={0}", tmpForumID );
			else
				yaf_BuildLink.Redirect( ForumPages.posts, "t={0}", tmpTopicID );

			// END CHANGED BAI 30.01.2004
		}


		private void UnDelete_Click( object sender, EventArgs e )
		{
			if ( !CanUnDeletePost )
				return;


			// CHANGED BAI 30.01.2004

			//Create objects for easy access
			object tmpMessageID = msg ["MessageID"];
			object tmpForumID = msg ["ForumID"];
			object tmpTopicID = msg ["TopicID"];


			// Delete message. If it is the last message of the topic, the topic is also deleted
			//DB.message_delete(tmpMessageID, isModeratorChanged);

			// retrieve topic information.
			DataRow topic = YAF.Classes.Data.DB.topic_info( tmpTopicID );

			//If topic has been deleted, redirect to topic list for active forum, else show remaining posts for topic
			if ( topic == null )
				yaf_BuildLink.Redirect( ForumPages.topics, "f={0}", tmpForumID );
			else
				yaf_BuildLink.Redirect( ForumPages.posts, "t={0}", tmpTopicID );

			// END CHANGED BAI 30.01.2004
		}


		protected void Cancel_Click( object sender, System.EventArgs e )
		{
			if ( Request.QueryString ["t"] != null || Request.QueryString ["m"] != null )
			{
				// reply to existing topic or editing of existing topic
				yaf_BuildLink.Redirect( ForumPages.posts, "t={0}", PageContext.PageTopicID );
			}
			else
			{
				// new topic -- cancel back to forum
				yaf_BuildLink.Redirect( ForumPages.topics, "f={0}", PageContext.PageForumID );
			}
		}
		protected string GetActionText()
		{
			if ( Request.QueryString ["action"].ToLower() == "delete" )
			{
				return GetText( "SAVE" );
			}
			else
			{
				return GetText( "SAVE" );
			}
		}

		protected string FormatBody( object o )
		{
			DataRowView row = ( DataRowView ) o;
			string html = FormatMsg.FormatMessage( row ["Message"].ToString(), new MessageFlags( Convert.ToInt32( row ["Flags"] ) ) );

			string sig = row ["Signature"].ToString();
			if ( sig != string.Empty )
			{
				sig = FormatMsg.FormatMessage( sig, new MessageFlags() );
				html += "<br/><hr noshade/>" + sig;
			}

			return html;
		}

		public void DeleteAllPosts_CheckedChanged1( object sender, EventArgs e )
		{
			ViewState ["delAll"] = ( ( CheckBox ) sender ).Checked;
		}
	}
}
