/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustomAttributes.Controllers
{
  public class Security : Controller
  {
    private static readonly HttpClient client = new HttpClient();
    private const string KEY = "Authorization";

    protected async Task<bool> IsAuthorized(string urn)
    {
      // Authorization is required
      if (!base.Request.Headers.ContainsKey(KEY))
      {
        base.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return false;
      }

      // use Autodesk Forge to get permissions...
      client.DefaultRequestHeaders.Remove(KEY);
      client.DefaultRequestHeaders.Add(KEY, base.Request.Headers[KEY][0]);

      // now we need to call one Forge endpoint to check our credentials
      // the metadata endpoints seems the fastest!
      // https://developer.autodesk.com/en/docs/model-derivative/v2/reference/http/urn-metadata-GET/
      //
      // for the record, also checked manifest, but that's is taking longer to return
      // https://developer.autodesk.com/en/docs/model-derivative/v2/reference/http/urn-manifest-GET/

      // Requesting only the Header seems a few milisecs faster, so let's use it
      HttpRequestMessage req = new HttpRequestMessage();
      req.RequestUri = new System.Uri(
        string.Format("https://developer.api.autodesk.com/modelderivative/v2/designdata/{0}/metadata", urn));
      req.Method = new HttpMethod("GET");
      HttpResponseMessage res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

      // a few milisecs slower...
      // HttpResponseMessage response = await client.GetAsync(
      //   string.Format("https://developer.api.autodesk.com/modelderivative/v2/designdata/{0}/metadata", urn));

      if (res.StatusCode != HttpStatusCode.OK)
      {
        base.Response.StatusCode = (int)res.StatusCode;
        return false;
      }

      // good to go!
      return true;
    }
  }
}
