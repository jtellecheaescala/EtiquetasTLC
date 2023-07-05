IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'Sitio_Web' AND Object_ID = Object_ID(N'Filiales'))
BEGIN
	ALTER TABLE Filiales
	ADD Sitio_Web varchar(max);
END
GO

IF NOT EXISTS(SELECT * FROM Variables WHERE Campo = 'Etiquetas_WS_Tope')
BEGIN
	INSERT INTO Variables (Campo, Valor, Tipo, Fecha_Hora_Modif)
	VALUES ('Etiquetas_WS_Tope', 0, 'N', GETDATE())
END
GO

IF NOT EXISTS(SELECT * FROM Variables WHERE Campo = 'Etiquetas_WS_Adicionales')
BEGIN
	INSERT INTO Variables (Campo, Valor, Tipo, Fecha_Hora_Modif)
	VALUES ('Etiquetas_WS_Adicionales', 0, 'N', GETDATE())
END
GO

