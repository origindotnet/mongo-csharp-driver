﻿/* Copyright 2010 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient.Internal {
    internal class MongoInsertMessage : MongoRequestMessage {
        #region private fields
        private string collectionFullName;
        private long firstDocumentStartPosition;
        private long lastDocumentStartPosition;
        #endregion

        #region constructors
        internal MongoInsertMessage(
            string collectionFullName
        )
            : base(MessageOpcode.Insert) {
            this.collectionFullName = collectionFullName;
        }
        #endregion

        #region internal methods
        internal void AddDocument<I>(
            I document
        ) {
            if (memoryStream == null) {
                // create a base message with no documents added yet
                WriteTo(new MemoryStream()); // assigns values to inherited memoryStream and binaryWriter fields
                firstDocumentStartPosition = memoryStream.Position;
            }

            lastDocumentStartPosition = memoryStream.Position;
            var serializer = new BsonSerializer(typeof(I));
            var bsonWriter = BsonWriter.Create(binaryWriter);
            serializer.WriteObject(bsonWriter, document);
            BackpatchMessageLength(binaryWriter);
        }

        internal byte[] RemoveLastDocument() {
            var lastDocumentLength = (int) (memoryStream.Position - lastDocumentStartPosition);
            var lastDocument = new byte[lastDocumentLength];
            memoryStream.Position = lastDocumentStartPosition;
            memoryStream.Read(lastDocument, 0, lastDocumentLength);

            memoryStream.Position = lastDocumentStartPosition;
            memoryStream.SetLength(lastDocumentStartPosition);
            BackpatchMessageLength(binaryWriter);

            return lastDocument;
        }

        internal void Reset(
            byte[] bsonDocument // as returned by RemoveLastDocument
        ) {
            memoryStream.Position = firstDocumentStartPosition;
            memoryStream.SetLength(firstDocumentStartPosition);

            lastDocumentStartPosition = memoryStream.Position;
            memoryStream.Write(bsonDocument, 0, bsonDocument.Length);
            BackpatchMessageLength(binaryWriter);
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter binaryWriter
        ) {
            binaryWriter.Write((int) 0); // reserved
            WriteCStringTo(binaryWriter, collectionFullName);
            // documents to be added later by calling AddDocument
        }
        #endregion
    }
}
