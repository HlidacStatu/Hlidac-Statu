using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using ProtoBuf.Grpc;

namespace GrpcProtobufs
{
    [DataContract]
    public class AutocompleteResponse
    {
        // nejde jednoduše použít přímo autocomplete objekt, protože není "rozpadnutý" na DataMembers
        [DataMember(Order = 1)]
        public string JsonMessage { get; set; } 
    }

    [DataContract]
    public class AutocompleteRequest
    {
        [DataMember(Order = 1)]
        public string Query { get; set; }
    }

    [ServiceContract]
    public interface IAutocompleteGrpc
    {
        [OperationContract]
        Task<AutocompleteResponse> AutocompleteAsync(AutocompleteRequest request, CallContext context = default);
        
        [OperationContract]
        Task<AutocompleteResponse> TestAutocompleteAsync(AutocompleteRequest request, CallContext context = default);
    }
}