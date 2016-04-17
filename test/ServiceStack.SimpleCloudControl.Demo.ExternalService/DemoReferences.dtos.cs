/* Options:
Date: 2016-04-15 21:22:50
Version: 4.055
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://Being:9999/

//GlobalNamespace: 
//MakePartial: True
//MakeVirtual: True
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//IncludeTypes: 
//ExcludeTypes: 
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.SimpleCloudControl.Demo.ExternalService;


namespace ServiceStack.SimpleCloudControl.Demo.ExternalService
{

    public class SubmitFoo : IReturn<FooResponse>
    {
        public string Name { get; set; }
    }

    public class FooResponse
    {
        public bool Queued { get; set; }
        public string Name { get; set; }
    }

    public partial class SubmitFooExternal
        : IReturn<string>
    {
        public virtual string Name { get; set; }
    }
}

