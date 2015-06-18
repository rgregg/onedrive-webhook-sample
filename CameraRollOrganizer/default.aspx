<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Async="true" Inherits="CameraRollOrganizer._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OneDrive Camera Roll Organizer (sample)</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>OneDrive Camera Roll Organizer</h1>

        <asp:Panel runat="server" ID="panelNoCurrentUser">
            <p>Sign in to your OneDrive to enable automatic camera roll organization. This sample application
            uses Webhooks and OneDrive API.</p>

            <p><asp:HyperLink runat="server" ID="linkToSignIn">Sign in to OneDrive</asp:HyperLink></p>
        </asp:Panel>

        <asp:Panel runat="server" ID="panelLoggedIn" Visible="false">
            <p>Account owner: <asp:Label runat="server" ID="labelAccountDisplayName"></asp:Label></p>
            <p>ID: <asp:Label runat="server" ID="labelAccountId"></asp:Label></p>
            <p>Photos organized: <asp:Label runat="server" ID="labelPhotosOrganizedCount"></asp:Label></p>
            <p>Webhooks Received: <asp:Label runat="server" ID="labelWebhooksReceived"></asp:Label></p>
            <p>Folder Format: <asp:TextBox runat="server" ID="textBoxFolderFormatString"></asp:TextBox></p>

            <p><a href="/signout">Sign Out</a></p>
            <p><asp:Button runat="server" ID="buttonSaveChanges" Text="Save Changes" OnClick="buttonSaveChanges_Click" /></p>
        </asp:Panel>
    </div>
    </form>
</body>
</html>
