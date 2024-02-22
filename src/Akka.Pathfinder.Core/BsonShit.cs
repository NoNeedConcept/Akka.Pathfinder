using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace Akka.Pathfinder.Core;
public static class BsonShit
{
    public static void Register()
    {
        ConventionRegistry.Register(
            "DictionaryRepresentationConvention",
            new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) },
            _ => true);

        ConventionRegistry.Register(
            "IgnoreExtraElements",
             new ConventionPack { new IgnoreExtraElementsConvention(true) }, 
             _ => true);
            
        BsonSerializer.RegisterSerializer(new UInt32Serializer(BsonType.Int64, new RepresentationConverter(false, true)));
        //BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }
}