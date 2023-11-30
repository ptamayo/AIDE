using System.ComponentModel;

namespace Aide.Admin.Domain.Enumerations
{
	public enum EnumClaimTypeId
	{
		[Description("Siniestro de atención en sucursal")]
		Siniestro = 1,

		[Description("Orden de servicio")]
		OrdenDeServicio = 2,

		[Description("Vale de colisión")]
		Colision = 3,

		[Description("Desmonta y monta")]
		DesmontaMonta = 4,

		[Description("Reparación de parabrisas")]
		Reparacion = 5,

		[Description("Calibración de parabrisas")]
		Calibracion = 6,

        [Description("Verificación vehicular")]
        Verificacion = 7
    }
}
