/* Options:
Date: 2016-04-15 21:32:46
Version: 4.055
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:9998/

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
using ServiceStack.SimpleCloudControl.ExternalService;


namespace ServiceStack.SimpleCloudControl.ExternalService
{

    public partial class SubmitBarExternal : IReturn<string>
    {
        public virtual string Name { get; set; }
    }
}

