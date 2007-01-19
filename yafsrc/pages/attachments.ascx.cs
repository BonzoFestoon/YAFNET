using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using YAF.Classes.Utils;
using YAF.Classes.Data;

namespace YAF.Pages // YAF.Pages
{
	/// <summary>
	/// Summary description for attachments.
	/// </summary>
	public partial class attachments : YAF.Classes.Base.ForumPage
	{
		private DataRow forum, topic;

		public attachments()
			: base( "ATTACHMENTS" )
		{
		}

		protected void Page_Load( object sender, System.EventArgs e )
		{
			using ( DataTable dt = YAF.Classes.Data.DB.forum_list( PageContext.PageBoardID, PageContext.PageForumID ) )
				forum = dt.Rows [0];
			topic = YAF.Classes.Data.DB.topic_info( PageContext.PageTopicID );

			if ( !IsPostBack )
			{
				if ( !PageContext.ForumModeratorAccess && !PageContext.ForumUploadAccess )
					yaf_BuildLink.AccessDenied();

				if ( !PageContext.ForumReadAccess )
					yaf_BuildLink.AccessDenied();

				if ( ( ( int ) topic ["Flags"] & ( int ) YAF.Classes.Data.TopicFlags.Locked ) == ( int ) YAF.Classes.Data.TopicFlags.Locked )
					yaf_BuildLink.AccessDenied(/*"The topic is closed."*/);

				if ( ( ( int ) forum ["Flags"] & ( int ) YAF.Classes.Data.ForumFlags.Locked ) == ( int ) YAF.Classes.Data.ForumFlags.Locked )
					yaf_BuildLink.AccessDenied(/*"The forum is closed."*/);

				// Check that non-moderators only edit messages they have written
				if ( !PageContext.ForumModeratorAccess )
					using ( DataTable dt = YAF.Classes.Data.DB.message_list( Request.QueryString ["m"] ) )
						if ( ( int ) dt.Rows [0] ["UserID"] != PageContext.PageUserID )
							yaf_BuildLink.AccessDenied(/*"You didn't post this message."*/);

				if ( PageContext.Settings.LockedForum == 0 )
				{
					PageLinks.AddLink( PageContext.BoardSettings.Name, YAF.Classes.Utils.yaf_BuildLink.GetLink( YAF.Classes.Utils.ForumPages.forum ) );
					PageLinks.AddLink( PageContext.PageCategoryName, YAF.Classes.Utils.yaf_BuildLink.GetLink( YAF.Classes.Utils.ForumPages.forum, "c={0}", PageContext.PageCategoryID ) );
				}
				PageLinks.AddForumLinks( PageContext.PageForumID );
				PageLinks.AddLink( PageContext.PageTopicName, YAF.Classes.Utils.yaf_BuildLink.GetLink( YAF.Classes.Utils.ForumPages.posts, "t={0}", PageContext.PageTopicID ) );
				PageLinks.AddLink( GetText( "TITLE" ), "" );

				Back.Text = GetText( "BACK" );
				Upload.Text = GetText( "UPLOAD" );

				BindData();
			}
		}

		private void BindData()
		{
			DataTable dt = YAF.Classes.Data.DB.attachment_list( Request.QueryString ["m"], null, null );
			List.DataSource = dt;

			List.Visible = ( dt.Rows.Count > 0 ) ? true : false;

			DataBind();
		}

		protected void Delete_Load( object sender, System.EventArgs e )
		{
			( ( LinkButton ) sender ).Attributes ["onclick"] = String.Format( "return confirm('{0}')", GetText( "ASK_DELETE" ) );
		}

		private void Back_Click( object sender, System.EventArgs e )
		{
			YAF.Classes.Utils.yaf_BuildLink.Redirect( YAF.Classes.Utils.ForumPages.posts, "m={0}#{0}", Request.QueryString ["m"] );
		}

		private void List_ItemCommand( object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e )
		{
			switch ( e.CommandName )
			{
				case "delete":
					YAF.Classes.Data.DB.attachment_delete( e.CommandArgument );
					BindData();
					break;
			}
		}

		private void Upload_Click( object sender, System.EventArgs e )
		{
			try
			{
				CheckValidFile( File );
				SaveAttachment( Request.QueryString ["m"], File );
				BindData();
			}
			catch ( Exception x )
			{
				YAF.Classes.Data.DB.eventlog_create( PageContext.PageUserID, this, x );
				PageContext.AddLoadMessage( x.Message );
				return;
			}
		}

		private void CheckValidFile( HtmlInputFile file )
		{
			if ( file.PostedFile == null || file.PostedFile.FileName.Trim().Length == 0 || file.PostedFile.ContentLength == 0 )
				return;

			string filename = file.PostedFile.FileName;
			int pos = filename.LastIndexOfAny( new char [] { '/', '\\' } );
			if ( pos >= 0 )
				filename = filename.Substring( pos + 1 );
			pos = filename.LastIndexOf( '.' );
			if ( pos >= 0 )
			{
				switch ( filename.Substring( pos + 1 ).ToLower() )
				{
					default:
						break;
					case "asp":
					case "aspx":
					case "ascx":
					case "config":
					case "php":
					case "php3":
					case "js":
					case "vb":
					case "vbs":
						throw new Exception( String.Format( GetText( "fileerror" ), filename ) );
				}
			}
		}

		private void SaveAttachment( object messageID, HtmlInputFile file )
		{
			if ( file.PostedFile == null || file.PostedFile.FileName.Trim().Length == 0 || file.PostedFile.ContentLength == 0 )
				return;

			string sUpDir = Request.MapPath( YAF.Classes.Config.UploadDir );
			string filename = file.PostedFile.FileName;

			int pos = filename.LastIndexOfAny( new char [] { '/', '\\' } );
			if ( pos >= 0 ) filename = filename.Substring( pos + 1 );

			// verify the size of the attachment
			if ( PageContext.BoardSettings.MaxFileSize > 0 && file.PostedFile.ContentLength > PageContext.BoardSettings.MaxFileSize )
				throw new Exception( GetText( "ERROR_TOOBIG" ) );

			if ( PageContext.BoardSettings.UseFileTable )
			{
				YAF.Classes.Data.DB.attachment_save( messageID, filename, file.PostedFile.ContentLength, file.PostedFile.ContentType, file.PostedFile.InputStream );
			}
			else
			{
				file.PostedFile.SaveAs( String.Format( "{0}{1}.{2}", sUpDir, messageID, filename ) );
				YAF.Classes.Data.DB.attachment_save( messageID, filename, file.PostedFile.ContentLength, file.PostedFile.ContentType, null );
			}
		}

		#region Web Form Designer generated code
		override protected void OnInit( EventArgs e )
		{
			Back.Click += new EventHandler( Back_Click );
			Upload.Click += new EventHandler( Upload_Click );
			List.ItemCommand += new RepeaterCommandEventHandler( List_ItemCommand );
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
	}
}
