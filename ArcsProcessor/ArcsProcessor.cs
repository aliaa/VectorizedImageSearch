using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace SimilarImageSearch.Engine.ArcsProcessor
{
    public static class ArcsProcessor
    {
        public static SqlSingle CompareShapes(SqlArcRelation[] shape1ArcRelations, SqlArcRelation[] shape2ArcRelations, float MinAcceptableSimilarity)
        {
            throw new NotImplementedException();
        }
    }
}
