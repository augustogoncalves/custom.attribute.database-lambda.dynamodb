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
  [Route("api/[controller]")]
  public class AttributesController : Security
  {
    [HttpGet("{urn}")]
    public async Task<Model.Attributes> Get(string urn)
    {
      if (!await IsAuthorized(urn)) return null;

      // start the database
      Database.Attributes attributesDb = new Database.Attributes();

      // return the attributes
      return await attributesDb.GetAttributes(urn);
    }

    [HttpPost]
    public async Task Post([FromBody]Model.Attributes attributes)
    {
      if (!await IsAuthorized(attributes.URN)) return;
      
      // start the database
      Database.Attributes attributesDb = new Database.Attributes();

      // this attribute is already there?
      Model.Attributes existingAttributes = await attributesDb.GetAttributes(attributes.URN);
      if (existingAttributes != null)
      {
        // ops, already there
        base.Response.StatusCode = (int)HttpStatusCode.Conflict;
        return;
      }

      // save
      await attributesDb.SaveAttributes(attributes);
    }

    [HttpPut("{urn}")]
    public async void Put(string urn, [FromBody]Model.Attributes updatedAttributes)
    {
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
  }
}
