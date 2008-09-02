﻿//-----------------------------------------------------------------------
// <copyright file="IMessageTypeProvider.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOAuth.Messaging {
	using System;
	using System.Collections.Generic;

	internal interface IMessageTypeProvider {
		/// <summary>
		/// Analyzes an incoming request message payload to discover what kind of 
		/// message is embedded in it.
		/// </summary>
		/// <param name="fields">The name/value pairs that make up the message payload.</param>
		/// <returns>The <see cref="IProtocolMessage"/>-derived concrete class that
		/// this message can deserialize to.</returns>
		Type GetRequestMessageType(IDictionary<string, string> fields);

		/// <summary>
		/// Analyzes a response message payload to discover what kind of message is embedded in it.
		/// </summary>
		/// <param name="request">
		/// The message that was sent as a request that resulted in the response.
		/// </param>
		/// <param name="fields">The name/value pairs that make up the message payload.</param>
		/// <returns>The <see cref="IProtocolMessage"/>-derived concrete class that
		/// this message can deserialize to.</returns>
		Type GetResponseMessageType(IProtocolMessage request, IDictionary<string, string> fields);
	}
}