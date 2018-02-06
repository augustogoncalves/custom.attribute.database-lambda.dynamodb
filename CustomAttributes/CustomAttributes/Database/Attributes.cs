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

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CustomAttributes.Database
{
  public class Attributes
  {
    private const string TABLE_NAME = "AttributesTable";
    private static AmazonDynamoDBClient client;
    private DynamoDBContext DDBContext { get; set; }

    /// <summary>
    /// Database handler for Attributes
    /// </summary>
    public Attributes()
    {
      if (client == null)
      {
        AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
        if (System.Diagnostics.Debugger.IsAttached) clientConfig.ServiceURL = "http://localhost:8000"; // localhost testing only
        client = new AmazonDynamoDBClient(clientConfig);
      }

      AWSConfigsDynamoDB.Context.TypeMappings[typeof(Model.Attributes)] = new Amazon.Util.TypeMapping(typeof(Model.Attributes), TABLE_NAME);
      DynamoDBContextConfig config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
      DDBContext = new DynamoDBContext(client, config);
    }

    /// <summary>
    /// Removes the "urn:" prefix of all Forge URNs
    /// </summary>
    /// <param name="urn"></param>
    /// <returns></returns>
    private static string AdjustURN(string urn)
    {
      // from https://stackoverflow.com/a/33113820
      urn = urn.Replace('-', '+'); // 62nd char of encoding
      urn = urn.Replace('_', '/'); // 63rd char of encoding
      switch (urn.Length % 4) // Pad with trailing '='s
      {
        case 0: break; // No pad chars in this case
        case 2: urn += "=="; break; // Two pad chars
        case 3: urn += "="; break; // One pad char
        default:
          throw new System.Exception("Illegal base64url string!");
      }

      string decodedURN = Encoding.UTF8.GetString(Convert.FromBase64String(urn));
      if (decodedURN.IndexOf("urn:") == -1) return urn;
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedURN.Remove(0, 4)));
    }

    /// <summary>
    /// Save attribute information. 
    /// </summary>
    /// <param name="attributes">Must have, at least, the URN property</param>
    /// <returns></returns>
    public async Task<Model.Attributes> SaveAttributes(Model.Attributes attributes)
    {
      ListTablesResponse existingTables = await client.ListTablesAsync();
      if (!existingTables.TableNames.Contains(TABLE_NAME)) await SetupTable(client, TABLE_NAME, "URN");

      try
      {
        attributes.URN = AdjustURN(attributes.URN);

        await DDBContext.SaveAsync<Model.Attributes>(attributes);
        return attributes;
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error saving attributes: " + ex.Message);
      }
      return null;
    }


    /// <summary>
    /// Get attribute information for a given URN
    /// </summary>
    /// <param name="urn"></param>
    /// <returns></returns>
    public async Task<Model.Attributes> GetAttributes(string urn)
    {
      ListTablesResponse existingTables = await client.ListTablesAsync();
      if (!existingTables.TableNames.Contains(TABLE_NAME)) await SetupTable(client, TABLE_NAME, "URN");

      urn = AdjustURN(urn);

      try
      {
        return await DDBContext.LoadAsync<Model.Attributes>(urn);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error loading attributes: " + ex.Message);
      }
      return null;
    }

    /// <summary>
    /// Create table if it doesn't exist. Sample code from https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/LowLevelDotNetTableOperationsExample.html
    /// </summary>
    /// <param name="client"></param>
    /// <param name="tabelName"></param>
    /// <param name="primaryKey"></param>
    /// <param name="sortKey"></param>
    /// <returns></returns>
    internal static async Task<string> SetupTable(AmazonDynamoDBClient client, string tabelName, string primaryKey, string sortKey = null)
    {
      Console.WriteLine("\n*** Creating table ***");
      var request = new CreateTableRequest
      {
        AttributeDefinitions = new List<AttributeDefinition>()
              {
                  new AttributeDefinition
                  {
                      AttributeName = primaryKey,
                      AttributeType = "S"
                  }
              },
        KeySchema = new List<KeySchemaElement>
              {
                  new KeySchemaElement
                  {
                      AttributeName =primaryKey,
                      KeyType = "HASH" //Partition key
                  }
              },
        ProvisionedThroughput = new ProvisionedThroughput
        {
          ReadCapacityUnits = 5,
          WriteCapacityUnits = 6
        },
        TableName = tabelName
      };

      if (!string.IsNullOrWhiteSpace(sortKey))
      {
        request.AttributeDefinitions.Add(new AttributeDefinition()
        {
          AttributeName = sortKey,
          AttributeType = "S"
        });
        request.KeySchema.Add(new KeySchemaElement()
        {
          AttributeName = sortKey,
          KeyType = "RANGE" // Sort Key
        });
      }
      try
      {
        var response = await client.CreateTableAsync(request);

        var tableDescription = response.TableDescription;
        Console.WriteLine("{1}: {0} \t ReadsPerSec: {2} \t WritesPerSec: {3}",
                  tableDescription.TableStatus,
                  tableDescription.TableName,
                  tableDescription.ProvisionedThroughput.ReadCapacityUnits,
                  tableDescription.ProvisionedThroughput.WriteCapacityUnits);

        string status = tableDescription.TableStatus;
        Console.WriteLine(tabelName + " - " + status);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
      return tabelName;
    }
  }
}
