using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Template.Enums;

public enum TipoGiustificativo
{
    [Display(Name = "Donazione di Sangue")]
    DonazioneSangue = 1,

    [Display(Name = "Ferie / ROL")]
    FerieRol = 2,

    [Display(Name = "Ferie Straordinarie")]
    FerieStraordinarie = 10,

    [Display(Name = "Ore Lavorate")]
    OreLavorate = 3,

    [Display(Name = "Ore Recupero Godute")]
    OreRecuperoGodute = 4,

    [Display(Name = "Ore Recupero Maturate")]
    OreRecuperoMaturate = 5,

    [Display(Name = "Ore Viaggio")]
    OreViaggio = 6,

    [Display(Name = "Permesso Studio")]
    PermessoStudio = 7,

    [Display(Name = "Smart Working")]
    SmartWorking = 8,

    [Display(Name = "Straordinario")]
    Straordinario = 9
}
