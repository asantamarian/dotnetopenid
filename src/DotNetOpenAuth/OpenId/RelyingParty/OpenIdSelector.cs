﻿//-----------------------------------------------------------------------
// <copyright file="OpenIdSelector.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

[assembly: System.Web.UI.WebResource(DotNetOpenAuth.OpenId.RelyingParty.OpenIdSelector.EmbeddedScriptResourceName, "text/javascript")]
[assembly: System.Web.UI.WebResource(DotNetOpenAuth.OpenId.RelyingParty.OpenIdSelector.EmbeddedStylesheetResourceName, "text/css")]

namespace DotNetOpenAuth.OpenId.RelyingParty {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics.Contracts;
	using System.Globalization;
	using System.IdentityModel.Claims;
	using System.Linq;
	using System.Text;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.HtmlControls;
	using System.Web.UI.WebControls;
	using DotNetOpenAuth.ComponentModel;
	using DotNetOpenAuth.InfoCard;
	using DotNetOpenAuth.Messaging;

	/// <summary>
	/// An ASP.NET control that provides a user-friendly way of logging into a web site using OpenID.
	/// </summary>
	[ToolboxData("<{0}:OpenIdSelector runat=\"server\"></{0}:OpenIdSelector>")]
	[ParseChildren(true), PersistChildren(false)]
	public class OpenIdSelector : OpenIdRelyingPartyAjaxControlBase {
		/// <summary>
		/// The name of the manifest stream containing the OpenIdButtonPanel.js file.
		/// </summary>
		internal const string EmbeddedScriptResourceName = Util.DefaultNamespace + ".OpenId.RelyingParty.OpenIdSelector.js";

		/// <summary>
		/// The name of the manifest stream containing the OpenIdButtonPanel.css file.
		/// </summary>
		internal const string EmbeddedStylesheetResourceName = Util.DefaultNamespace + ".OpenId.RelyingParty.OpenIdSelector.css";

		/// <summary>
		/// The substring to append to the end of the id or name of this control to form the
		/// unique name of the hidden field that will carry the positive assertion on postback.
		/// </summary>
		private const string AuthDataFormKeySuffix = "_openidAuthData";

		#region ViewState keys

		/// <summary>
		/// The viewstate key to use for storing the value of the <see cref="Buttons"/> property.
		/// </summary>
		private const string ButtonsViewStateKey = "Buttons";

		/// <summary>
		/// The viewstate key to use for storing the value of the <see cref="AuthenticatedAsToolTip"/> property.
		/// </summary>
		private const string AuthenticatedAsToolTipViewStateKey = "AuthenticatedAsToolTip";

		#endregion

		#region Property defaults

		/// <summary>
		/// The default value for the <see cref="AuthenticatedAsToolTip"/> property.
		/// </summary>
		private const string AuthenticatedAsToolTipDefault = "We recognize you!";

		#endregion

		/// <summary>
		/// The OpenIdAjaxTextBox that remains hidden until the user clicks the OpenID button.
		/// </summary>
		private OpenIdAjaxTextBox textBox;

		/// <summary>
		/// The hidden field that will transmit the positive assertion to the RP.
		/// </summary>
		private HiddenField positiveAssertionField;

		/// <summary>
		/// A field to store the value to set on the <see cref="textBox"/> control after it's created.
		/// </summary>
		private bool downloadYuiLibrary = OpenIdAjaxTextBox.DownloadYahooUILibraryDefault;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenIdSelector"/> class.
		/// </summary>
		public OpenIdSelector() {
		}

		/// <summary>
		/// Occurs when an InfoCard has been submitted and decoded.
		/// </summary>
		public event EventHandler<ReceivedTokenEventArgs> ReceivedToken;

		/// <summary>
		/// Occurs when [token processing error].
		/// </summary>
		public event EventHandler<TokenProcessingErrorEventArgs> TokenProcessingError;

		/// <summary>
		/// Gets or sets the tool tip text that appears on the green checkmark when authentication succeeds.
		/// </summary>
		[Bindable(true), DefaultValue(AuthenticatedAsToolTipDefault), Localizable(true), Category(AppearanceCategory)]
		[Description("The tool tip text that appears on the green checkmark when authentication succeeds.")]
		public string AuthenticatedAsToolTip {
			get { return (string)(this.ViewState[AuthenticatedAsToolTipViewStateKey] ?? AuthenticatedAsToolTipDefault); }
			set { this.ViewState[AuthenticatedAsToolTipViewStateKey] = value ?? string.Empty; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the Yahoo! User Interface Library (YUI)
		/// will be downloaded in order to provide a login split button.
		/// </summary>
		/// <value>
		/// 	<c>true</c> to use a split button; otherwise, <c>false</c> to use a standard HTML button
		/// 	or a split button by downloading the YUI library yourself on the hosting web page.
		/// </value>
		/// <remarks>
		/// The split button brings in about 180KB of YUI javascript dependencies.
		/// </remarks>
		[Bindable(true), DefaultValue(OpenIdAjaxTextBox.DownloadYahooUILibraryDefault), Category(BehaviorCategory)]
		[Description("Whether a split button will be used for the \"log in\" when the user provides an identifier that delegates to more than one Provider.")]
		public bool DownloadYahooUILibrary {
			get {
				return this.textBox != null ? this.textBox.DownloadYahooUILibrary : this.downloadYuiLibrary;
			}

			set {
				// We don't just call EnsureChildControls() and then set the property on
				// this.textBox itself because (apparently) setting this property in the ASPX
				// page and thus calling this EnsureID() via EnsureChildControls() this early
				// results in no ID.
				if (this.textBox != null) {
					this.textBox.DownloadYahooUILibrary = value;
				} else {
					this.downloadYuiLibrary = value;
				}
			}
		}

		/// <summary>
		/// Gets the collection of buttons this selector should render to the browser.
		/// </summary>
		[PersistenceMode(PersistenceMode.InnerProperty)]
		public Collection<SelectorButton> Buttons {
			get {
				if (this.ViewState[ButtonsViewStateKey] == null) {
					var providers = new Collection<SelectorButton>();
					this.ViewState[ButtonsViewStateKey] = providers;
					return providers;
				} else {
					return (Collection<SelectorButton>)this.ViewState[ButtonsViewStateKey];
				}
			}
		}

		/// <summary>
		/// Gets a <see cref="T:System.Web.UI.ControlCollection"/> object that represents the child controls for a specified server control in the UI hierarchy.
		/// </summary>
		/// <returns>
		/// The collection of child controls for the specified server control.
		/// </returns>
		public override ControlCollection Controls {
			get {
				this.EnsureChildControls();
				return base.Controls;
			}
		}

		/// <summary>
		/// Gets the name of the open id auth data form key (for the value as stored at the user agent as a FORM field).
		/// </summary>
		/// <value>
		/// Usually a concatenation of the control's name and <c>"_openidAuthData"</c>.
		/// </value>
		protected override string OpenIdAuthDataFormKey {
			get { return this.UniqueID + AuthDataFormKeySuffix; }
		}

		/// <summary>
		/// Gets a value indicating whether some button in the selector will want
		/// to display the <see cref="OpenIdAjaxTextBox"/> control.
		/// </summary>
		protected virtual bool OpenIdTextBoxVisible {
			get { return this.Buttons.OfType<SelectorOpenIdButton>().Any(); }
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				foreach (var button in this.Buttons.OfType<IDisposable>()) {
					button.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
		/// </summary>
		protected override void CreateChildControls() {
			base.CreateChildControls();
			this.EnsureID();
			ErrorUtilities.VerifyInternal(!string.IsNullOrEmpty(this.UniqueID), "Control.EnsureID() failed to give us a unique ID.  Try setting an ID on the OpenIdSelector control.  But please also file this bug with the project owners.");

			var selectorButton = this.Buttons.OfType<SelectorInfoCardButton>().FirstOrDefault();
			if (selectorButton != null) {
				var selector = selectorButton.InfoCardSelector;
				selector.ClaimsRequested.Add(new ClaimType { Name = ClaimTypes.PPID });
				selector.ImageSize = InfoCardImageSize.Size60x42;
				selector.ReceivedToken += this.InfoCardSelector_ReceivedToken;
				selector.TokenProcessingError += this.InfoCardSelector_TokenProcessingError;
				this.Controls.Add(selector);
			}

			this.textBox = new OpenIdAjaxTextBox();
			this.textBox.ID = "openid_identifier";
			this.textBox.HookFormSubmit = false;
			this.textBox.ShowLogOnPostBackButton = true;
			this.textBox.DownloadYahooUILibrary = this.downloadYuiLibrary;
			this.Controls.Add(this.textBox);

			this.positiveAssertionField = new HiddenField();
			this.positiveAssertionField.ID = this.ID + AuthDataFormKeySuffix;
			this.Controls.Add(this.positiveAssertionField);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
		protected override void OnInit(EventArgs e) {
			base.OnInit(e);

			// We force child control creation here so that they can get postback events.
			EnsureChildControls();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.PreRender"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
		protected override void OnPreRender(EventArgs e) {
			base.OnPreRender(e);

			this.EnsureValidButtons();

			var css = new HtmlLink();
			css.Href = this.Page.ClientScript.GetWebResourceUrl(this.GetType(), EmbeddedStylesheetResourceName);
			css.Attributes["rel"] = "stylesheet";
			css.Attributes["type"] = "text/css";
			ErrorUtilities.VerifyHost(this.Page.Header != null, OpenIdStrings.HeadTagMustIncludeRunatServer);
			this.Page.Header.Controls.AddAt(0, css); // insert at top so host page can override

			// Import the .js file where most of the code is.
			this.Page.ClientScript.RegisterClientScriptResource(typeof(OpenIdSelector), EmbeddedScriptResourceName);

			// Provide javascript with a way to post the login assertion.
			const string PostLoginAssertionMethodName = "postLoginAssertion";
			const string PositiveAssertionParameterName = "positiveAssertion";
			const string ScriptFormat = @"window.{2} = function({0}) {{
	$('#{3}')[0].setAttribute('value', {0});
	{1};
}};";
			string script = string.Format(
				CultureInfo.InvariantCulture,
				ScriptFormat,
				PositiveAssertionParameterName,
				this.Page.ClientScript.GetPostBackEventReference(this, null, false),
				PostLoginAssertionMethodName,
				this.positiveAssertionField.ClientID);
			this.Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "Postback", script, true);

			this.PreloadDiscovery(this.Buttons.OfType<SelectorProviderButton>().Select(op => op.OPIdentifier).Where(id => id != null));
			this.textBox.Visible = this.OpenIdTextBoxVisible;
		}

		/// <summary>
		/// Sends server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter"/> object, which writes the content to be rendered on the client.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter"/> object that receives the server control content.</param>
		protected override void Render(HtmlTextWriter writer) {
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "OpenIdProviders");
			writer.RenderBeginTag(HtmlTextWriterTag.Ul);

			foreach (var button in this.Buttons) {
				button.RenderLeadingAttributes(writer);

				writer.RenderBeginTag(HtmlTextWriterTag.Li);

				writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
				writer.RenderBeginTag(HtmlTextWriterTag.A);

				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);

				button.RenderButtonContent(writer, this);

				writer.RenderEndTag(); // </div>

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-overlay");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);
				writer.RenderEndTag();

				writer.RenderEndTag(); // </div>
				writer.RenderEndTag(); // </a>
				writer.RenderEndTag(); // </li>
			}

			writer.RenderEndTag(); // </ul>

			if (this.textBox.Visible) {
				writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
				writer.AddAttribute(HtmlTextWriterAttribute.Id, "OpenIDForm");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);

				this.textBox.RenderControl(writer);

				writer.RenderEndTag(); // </div>
			}

			this.positiveAssertionField.RenderControl(writer);
		}

		/// <summary>
		/// Fires the <see cref="ReceivedToken"/> event.
		/// </summary>
		/// <param name="e">The token, if it was decrypted.</param>
		protected virtual void OnReceivedToken(ReceivedTokenEventArgs e) {
			Contract.Requires(e != null);
			ErrorUtilities.VerifyArgumentNotNull(e, "e");

			var receivedInfoCard = this.ReceivedToken;
			if (receivedInfoCard != null) {
				receivedInfoCard(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="E:TokenProcessingError"/> event.
		/// </summary>
		/// <param name="e">The <see cref="DotNetOpenAuth.InfoCard.TokenProcessingErrorEventArgs"/> instance containing the event data.</param>
		protected virtual void OnTokenProcessingError(TokenProcessingErrorEventArgs e) {
			Contract.Requires(e != null);
			ErrorUtilities.VerifyArgumentNotNull(e, "e");

			var tokenProcessingError = this.TokenProcessingError;
			if (tokenProcessingError != null) {
				tokenProcessingError(this, e);
			}
		}

		/// <summary>
		/// Handles the ReceivedToken event of the infoCardSelector control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="DotNetOpenAuth.InfoCard.ReceivedTokenEventArgs"/> instance containing the event data.</param>
		private void InfoCardSelector_ReceivedToken(object sender, ReceivedTokenEventArgs e) {
			this.Page.Response.SetCookie(new HttpCookie("openid_identifier", "infocard") {
				Path = this.Page.Request.ApplicationPath,
			});
			this.OnReceivedToken(e);
		}

		/// <summary>
		/// Handles the TokenProcessingError event of the infoCardSelector control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="DotNetOpenAuth.InfoCard.TokenProcessingErrorEventArgs"/> instance containing the event data.</param>
		private void InfoCardSelector_TokenProcessingError(object sender, TokenProcessingErrorEventArgs e) {
			this.OnTokenProcessingError(e);
		}

		/// <summary>
		/// Ensures the <see cref="Buttons"/> collection has a valid set of buttons.
		/// </summary>
		private void EnsureValidButtons() {
			foreach (var button in this.Buttons) {
				button.EnsureValid();
			}

			// Also make sure that there are appropriate numbers of each type of button.
			// TODO: code here
		}
	}
}
