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
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomAttributes.Database
{
  public class Attributes
  {
    private const string TABLE_NAME = "AttributesTable";
    private static AmazonDynamoDBClient client;

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
    }

    /// <summary>
    /// Save attribute information. 
    /// </summary>
    /// <param name="attributes">Must have, at least, the URN property</param>
    /// <returns></returns>
    public async Task<bool> SaveAttributes(Model.Attributes attributes)
    {
      ListTablesResponse existingTables = await client.ListTablesAsync();
      if (!existingTables.TableNames.Contains(TABLE_NAME)) await SetupTable(client, TABLE_NAME, "URN");

      try
      {
        // create a generic dictionary
        Dictionary<string, DynamoDBEntry> dic = new Dictionary<string, DynamoDBEntry>();
        dic.Add("URN", attributes.URN);
        dic.Add("Data", attributes.Data.ToString());

        // save as a DynamoDB document
        Table table = Table.LoadTable(client, TABLE_NAME);
        Document document = new Document(dic);
        await table.PutItemAsync(document);

        // all good!
        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error saving attributes: " + ex.Message);
      }
      return false;
    }


    /// <summary>
    /// Get attribute information for a given URN
    /// </summary>
    /// <param name="urn"></param>
    /// <returns></returns>
    public async Task<string> GetAttributes(string urn)
    {
      ListTablesResponse existingTables = await client.ListTablesAsync();
      if (!existingTables.TableNames.Contains(TABLE_NAME)) await SetupTable(client, TABLE_NAME, "URN");
          
      try
      {
        Table table = Table.LoadTable(client, TABLE_NAME);
        Document document = await table.GetItemAsync(urn);
        return document["Data"].AsString(); // will be parsed later
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error loading attributes: " + ex.Message);
      }
      return null;
    }

    /// <summary>
    /// Create table if it doesn't exist. 
    /// Sample code from https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/LowLevelDotNetTableOperationsExample.html
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
