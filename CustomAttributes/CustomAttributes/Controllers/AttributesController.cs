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
using System.Threading.Tasks;

namespace CustomAttributes.Controllers
{
  [Route("api/[controller]")]
  public class AttributesController : Security
  {
    [HttpGet("{urn}")]
    public async Task<JsonResult> Get(string urn)
    {
      if (!await IsAuthorized(urn)) return new JsonResult(new { Error = "Invalid or expired access token" });

      // start the database
      Database.Attributes attributesDb = new Database.Attributes();

      // return the attributes
      string attributes = await attributesDb.GetAttributes(urn);
      var res = new { URN = urn, Data = Newtonsoft.Json.JsonConvert.DeserializeObject(attributes) };

      return new JsonResult(res);
    }

    [HttpPost]
    public async Task Post([FromBody]Model.Attributes newAttributes)
    {
      if (!isValidInput(newAttributes)) return;
      if (!isValidJsonString(newAttributes.Data.ToString())) return;
      if (!await IsAuthorized(newAttributes.URN)) return;

      // start the database
      Database.Attributes attributesDb = new Database.Attributes();

      // this attribute is already there?
      string existingAttributes = await attributesDb.GetAttributes(newAttributes.URN);
      if (existingAttributes != null)
      {
        // ops, already there
        base.Response.StatusCode = (int)HttpStatusCode.Conflict;
        return;
      }

      // save
      await attributesDb.SaveAttributes(newAttributes);
    }

    [HttpPut("{urn}")]
    public async Task Put(string urn, [FromBody]Model.Attributes updatedAttributes)
    {
      if (!isValidInput(updatedAttributes)) return;
      if (!isValidJsonString(updatedAttributes.Data.ToString())) return;

      if (!urn.Equals(updatedAttributes.URN))
      {
        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
      }


      if (!await IsAuthorized(urn)) return;

      // start the database
      Database.Attributes attributesDb = new Database.Attributes();

      /*
       * should we check if the attribute is there or not?
       * if is not there, let's just create it then...
       * 
      // this attribute is already there?
      Model.Attributes existingAttributes = await attributesDb.GetAttributes(urn);
      if (existingAttributes == null)
      {
        // ops, not there yet...
        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
      }
      */

      await attributesDb.SaveAttributes(updatedAttributes);
    }

    private bool isValidInput(Model.Attributes input)
    {
      if (input == null || string.IsNullOrWhiteSpace(input.URN)
        || input.Data == null || string.IsNullOrWhiteSpace(input.Data.ToString()))
      {
        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return false;
      }
      return true;
    }

    private bool isValidJsonString(string jsonInput)
    {
      try
      {
        object jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonInput);
        return true;
      }
      catch
      {
        base.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return false;
      }
    }
  }
}
