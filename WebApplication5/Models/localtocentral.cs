using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class localtocentral
    {
        public string Material { get; set; }
        public HttpPostedFileBase CertificationFile { get; set; }
        //public string CertificationFileName { get; set; }

        //public string StoreUploads { get; set; }
        //public string CertificationFilePath { get; set; }

        public List<SavetoCentral> savetoCentrals { get; set; }

        public List<TempSelectedMaterial> tempSelectedMaterials { get; set; }
    }
}