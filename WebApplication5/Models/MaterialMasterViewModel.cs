using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class MaterialMasterViewModel
    {
        public List<AssetType> AssetTypes { get; set; }
        public MaterialMasterList MaterialMaster { get; set; }
    }
}