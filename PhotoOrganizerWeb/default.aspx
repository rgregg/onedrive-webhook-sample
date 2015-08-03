<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Async="true" Inherits="PhotoOrganizerWeb._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OneDrive Camera Roll Organizer (sample)</title>
    <link href="styles.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>Camera Roll Organizer</h1>

        <asp:Panel runat="server" ID="panelNoCurrentUser">
            <p>Sign in to your OneDrive to enable automatic camera roll organization. This sample application
            uses Webhooks and OneDrive API.</p>

            <p><asp:HyperLink runat="server" ID="linkToSignIn">Sign in to OneDrive</asp:HyperLink></p>
        </asp:Panel>

        <asp:Panel runat="server" ID="panelLoggedIn" Visible="false">
            <p>Hi <asp:Label runat="server" ID="labelAccountDisplayName"></asp:Label>. This web app can keep 
                your photos organized for you! Whenever new files are added to your Camera Roll folder in 
                OneDrive, we'll automatically move them into a folder based on the date the photo was taken. 
                Files that don't have a date taken won't be moved.</p>

            <p>Note: Right now while we're testing this stuff, we're only messing around with files in the 
                <a href="https://onedrive.live.com/?id=apps" target="_blank"><i>/Apps/Webhook Sample</i></a> 
                folder. No pre-release harm will come to your camera roll. Feel free to drop files into the 
                <i>Webhook Sample</i> folder and see what happens! If you don't have this folder, 
                <a href="/api/action/createfile" target="_blank">click here to create it</a>.</p>
            
            <h3>Configuration</h3>
            <p>You can change the way camera roll organizer works on your account. These changes only apply
                to items not already organized.</p>
            <p>Destination Folder Name: <asp:TextBox runat="server" ID="textBoxFolderFormatString" Width="200px"></asp:TextBox><br />
                <small>This is passed into String.Format(format, DateTimeOfFile), so you can use {0} or {0:format} to customize the folder names.</small>
            </p>

            <p><asp:CheckBox runat="server" ID="checkBoxEnableAccount" Text="Enable File Moves" /></p>
            <p><asp:Button runat="server" ID="buttonSaveChanges" Text="Save Changes" OnClick="buttonSaveChanges_Click" /></p>
            <asp:Label runat="server" ID="labelErrors" ForeColor="Red" Text=""></asp:Label>

            <h3>Statistics</h3>
            <p>Photos organized: <asp:Label runat="server" ID="labelPhotosOrganizedCount"></asp:Label></p>
            <p>Messages received: <asp:Label runat="server" ID="labelWebhooksReceived"></asp:Label></p>

            <h3>Commands</h3>
            <p><a href="/signout">Sign Out</a></p>
            <p><a href="/api/action/subscriptions?path=jump/apps/webhook%20sample"  target="_blank">View existing subscriptions</a></p>
        </asp:Panel>
    </div>
    </form>
</body>
</html>
