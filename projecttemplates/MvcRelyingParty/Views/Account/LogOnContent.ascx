<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<p>Login using an account you already use. </p>
<%= Html.ValidationSummary("Login was unsuccessful. Please correct the errors and try again.") %>

<% using (Html.BeginForm("LogOnPostAssertion", "Account", FormMethod.Post, new { target = "_top" })) { %>
<%= Html.AntiForgeryToken() %>
<%= Html.Hidden("ReturnUrl", Request.QueryString["ReturnUrl"], new { id = "ReturnUrl" }) %>
<%= Html.Hidden("openid_openidAuthData") %>
<div>
	<ul class="OpenIdProviders">
		<li id="https://www.google.com/accounts/o8/id" class="OPButton"><a href="#"><div><div>
		<img src="/Content/images/google.gif" class="OPLogo" />
		<img src="<%= Page.ClientScript.GetWebResourceUrl(typeof(DotNetOpenAuth.OpenId.RelyingParty.OpenIdSelector), "DotNetOpenAuth.OpenId.RelyingParty.login_success.png") %>" class="loginSuccess" title="Authenticated as {0}" />
		</div><div class="ui-widget-overlay"></div></div></a>
		</li>
		<li id="https://me.yahoo.com/" class="OPButton"><a href="#"><div><div>
		<img src="/Content/images/yahoo.gif" class="OPLogo" />
		<img src="<%= Page.ClientScript.GetWebResourceUrl(typeof(DotNetOpenAuth.OpenId.RelyingParty.OpenIdSelector), "DotNetOpenAuth.OpenId.RelyingParty.login_success.png") %>" class="loginSuccess" title="Authenticated as {0}" />
		</div><div class="ui-widget-overlay"></div></div></a>
		</li>
		<li id="OpenIDButton" class="OPButton"><a href="#"><div><div>
		<img src="/Content/images/openid.gif" class="OPLogo" />
		<img src="<%= Page.ClientScript.GetWebResourceUrl(typeof(DotNetOpenAuth.OpenId.RelyingParty.OpenIdSelector), "DotNetOpenAuth.OpenId.RelyingParty.login_success.png") %>" class="loginSuccess" title="Authenticated as {0}" />
		</div><div class="ui-widget-overlay"></div></div></a>
		</li>
	</ul>
	<div style="display: none" id="OpenIDForm">
		<span class="OpenIdAjaxTextBox" style="display: inline-block; position: relative; font-size: 16px">
			<input name="openid_identifier" id="openid_identifier" size="40" style="padding-left: 18px; border-style: solid; border-width: 1px; border-color: lightgray" />
		</span>
	</div>
</div>
<% } %>
