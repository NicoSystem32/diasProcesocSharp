//Metodo para calcular los dias en proceso y dias en proceso por estado
private List<InfoFormFiniquitos> CalculodeDiasFepyDcd(List<InfoFormFiniquitos> data)
        {
            IEnumerable<TrazabilidadModel> TodasLasTrazas;
            var db = dbConnection();
            var procedure = "F_SP_DETALLE_TRAZA_FCP_PLUS";
            OracleDynamicParameters values = new OracleDynamicParameters();
            values.Add("p_dcd_codigo", null);
            values.Add("p_fcp_codigo", null);
            values.Add("p_dfdc_codigo", null);
            values.Add("c_Resp", dbType: (OracleMappingType)OracleDbType.RefCursor, direction: ParameterDirection.Output);
            TodasLasTrazas = db.Query<TrazabilidadModel>(procedure, values, commandType: CommandType.StoredProcedure);
            var trazasAjustadas = new List<TrazabilidadModel>();

            try
            {
                foreach (var traza in data)
                {


                    List<TrazabilidadModel> trazabilidades = TodasLasTrazas.Where(x => x.HSTFCP_ID_FCP == traza.NUMFDC && x.HSTFCP_ID_DFDC == traza.ID_DFDC && x.HSTFCP_ID_DCD == traza.DCD_CODIGO).OrderBy(x => x.CMP_TRAZA_FECHA).ToList();
                    int diasParaAjustar = -1;

                    if (trazabilidades.Count == 0)
                    {
                        List<TrazabilidadModel> trazabilidadVacias = new List<TrazabilidadModel>();
                    }
                    else
                    {

                        //Debe ser el mismo que el de FiniquitosRepo.cs

                        foreach (var item in trazabilidades)
                        {
                            if (item.CMP_TRAZA_OBSERVACION.Equals("Aprobada"))
                                item.CMP_TRAZA_OBSERVACION = ("Pendiente Por Finiquito");

                            if (item.CMP_TRAZA_ESTADO_DOC.Equals("Aprobada"))
                                item.CMP_TRAZA_ESTADO_DOC = ("Pendiente Por Finiquito");
                        }
                        if (trazabilidades.Count == 2)
                        {
                            foreach (var item in trazabilidades)
                            {
                                if (item.CMP_TRAZA_ESTADO_DOC.Equals("Pendiente Por Finiquito"))
                                {

                                    item.DIASPROCESO = 0;
                                    item.DIASPROCESOESTADO = Math.Floor(((trazabilidades.FindAll(x => x.CMP_TRAZA_ESTADO_DOC == "Con Documentos recibidos – Finiquitos").ToList())[0].CMP_TRAZA_FECHA - item.CMP_TRAZA_FECHA).TotalDays);

                                }
                                if (item.CMP_TRAZA_ESTADO_DOC.Equals("Con Documentos recibidos – Finiquitos"))
                                {
                                    item.DIASPROCESO = Math.Round((DateTime.Now - item.CMP_TRAZA_FECHA).TotalDays);
                                    item.DIASPROCESOESTADO = Math.Round((DateTime.Now - item.CMP_TRAZA_FECHA).TotalDays);

                                }
                            }
                        }
                        else
                        {
                            int contador = 1;
                            //Inicio de calculo
                            var estadoFinal = trazabilidades[trazabilidades.Count - 1];

                            for (int i = 0; i < trazabilidades.Count; i++)
                            {

                                if (trazabilidades[i].HSTFCP_ID != estadoFinal.HSTFCP_ID)
                                {
                                    switch (trazabilidades[i].CMP_TRAZA_ESTADO_DOC)
                                    {
                                        case "Pendiente Por Finiquito":
                                            trazabilidades[i].DIASPROCESOESTADO = Math.Round((trazabilidades[i + 1].CMP_TRAZA_FECHA - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i].DIASPROCESO = 0;
                                            break;

                                        case "Con Documentos recibidos – Finiquitos":

                                            switch (trazabilidades[i - 1].CMP_TRAZA_ESTADO_DOC)
                                            {
                                                case "Pendiente Por Finiquito":
                                                    diasParaAjustar = OperadorDias(trazabilidades[i].CMP_TRAZA_FECHA, trazabilidades[i + 1].CMP_TRAZA_FECHA);

                                                    trazabilidades[i].DIASPROCESO = 0;
                                                    trazabilidades[i].DIASPROCESOESTADO = DiasNegativos(Math.Round((trazabilidades[i + 1].CMP_TRAZA_FECHA - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));
                                                    break;
                                                case "En Revisión Secretaría Técnica – Finiquitos":
                                                    trazabilidades[i].DIASPROCESO = 0;
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Ceiling((trazabilidades[i + 1].CMP_TRAZA_FECHA - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    break;
                                                case "Demostración Incompleta - Finiquitos":


                                                    trazabilidades[i].DIASPROCESO = Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);


                                                    trazabilidades[i - 1].DIASPROCESO = 0;
                                                    trazabilidades[i - 1].DIASPROCESOESTADO = 0;
                                                    break;

                                                case "En Revisión Recaudos y Pagos – Finiquitos":

                                                    trazabilidades[i - 1].DIASPROCESO = Math.Ceiling((trazabilidades[i - 1].CMP_TRAZA_FECHA - trazabilidades[i - 2].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i - 1].DIASPROCESOESTADO = Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);

                                                    trazabilidades[i].DIASPROCESO = 0;
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);
                                                    break;
                                                default:
                                                    break;
                                            }

                                            break;
                                        case "En Revisión Recaudos y Pagos – Finiquitos":

                                            diasParaAjustar = OperadorDias(trazabilidades[i].CMP_TRAZA_FECHA, trazabilidades[i + 1].CMP_TRAZA_FECHA);
                                            trazabilidades[i].DIASPROCESO = DiasNegativos(Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));
                                            trazabilidades[i].DIASPROCESOESTADO = 0;
                                            break;
                                        case "En Revisión Secretaría Técnica – Finiquitos":

                                            diasParaAjustar = OperadorDias(trazabilidades[i - 2].CMP_TRAZA_FECHA, trazabilidades[i - 1].CMP_TRAZA_FECHA);
                                            trazabilidades[i - 1].DIASPROCESO = DiasNegativos(Math.Ceiling((trazabilidades[i - 1].CMP_TRAZA_FECHA - trazabilidades[i - 2].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));
                                            trazabilidades[i - 1].DIASPROCESOESTADO = DiasNegativos(Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));

                                            diasParaAjustar = OperadorDias(trazabilidades[i - 2].CMP_TRAZA_FECHA, trazabilidades[i].CMP_TRAZA_FECHA);

                                            trazabilidades[i].DIASPROCESO = DiasNegativos(Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 2].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));
                                            trazabilidades[i].DIASPROCESOESTADO = Math.Round((trazabilidades[i + 1].CMP_TRAZA_FECHA - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                            break;
                                        case "Demostración Aprobada – Finiquitos":
                                            trazabilidades[i].DIASPROCESO = Math.Ceiling((trazabilidades[i - 3].CMP_TRAZA_FECHA - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i].DIASPROCESOESTADO = 0;
                                            break;
                                        case "Trámite Rechazado – Finiquitos":
                                            trazabilidades[i].DIASPROCESO = 0;
                                            trazabilidades[i].DIASPROCESOESTADO = 0;
                                            break;
                                        case "Demostración Incompleta - Finiquitos":

                                            trazabilidades[i].DIASPROCESO = 0;
                                            trazabilidades[i].DIASPROCESOESTADO = 0;

                                            switch (trazabilidades[i - 1].CMP_TRAZA_ESTADO_DOC)
                                            {
                                                case "Con Documentos recibidos – Finiquitos":
                                                    diasParaAjustar = OperadorDias(trazabilidades[i].CMP_TRAZA_FECHA, DateTime.Now);
                                                    trazabilidades[i - 1].DIASPROCESO = trazabilidades[i - 1].DIASPROCESOESTADO;
                                                    //trazabilidades[i-1].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);

                                                    break;
                                                default:
                                                    break;
                                            }




                                            break;
                                        default:
                                            break;
                                    }

                                }
                                else
                                {
                                    switch (trazabilidades[i].CMP_TRAZA_ESTADO_DOC)
                                    {
                                        case "Con Documentos recibidos – Finiquitos":

                                            switch (trazabilidades[i - 1].CMP_TRAZA_ESTADO_DOC)
                                            {

                                                case "En Revisión Secretaría Técnica – Finiquitos":
                                                    trazabilidades[i - 1].DIASPROCESO = Math.Ceiling((trazabilidades[i - 1].CMP_TRAZA_FECHA - trazabilidades[i - 3].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i - 1].DIASPROCESOESTADO = Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);

                                                    trazabilidades[i].DIASPROCESO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    break;

                                                case "En Revisión Recaudos y Pagos – Finiquitos":
                                                    //trazabilidades[i - 1].DIASPROCESO = 0;
                                                    trazabilidades[i - 1].DIASPROCESOESTADO = Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);

                                                    trazabilidades[i].DIASPROCESO = Math.Ceiling((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    break;

                                                case "Demostración Incompleta - Finiquitos":

                                                    trazabilidades[i].DIASPROCESO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    break;


                                                default:
                                                    break;
                                            }

                                            diasParaAjustar = OperadorDias(trazabilidades[i].CMP_TRAZA_FECHA, DateTime.Now);
                                            trazabilidades[i].DIASPROCESO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);

                                            break;
                                        case "En Revisión Recaudos y Pagos – Finiquitos":

                                            trazabilidades[i - 1].DIASPROCESO = 0;
                                            trazabilidades[i - 1].DIASPROCESOESTADO = Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);

                                            //configurado
                                            trazabilidades[i].DIASPROCESO = Math.Round((DateTime.Now - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                            break;
                                        case "En Revisión Secretaría Técnica – Finiquitos":

                                            trazabilidades[i - 1].DIASPROCESO = Math.Ceiling((trazabilidades[i - 1].CMP_TRAZA_FECHA - trazabilidades[i - 2].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i - 1].DIASPROCESOESTADO = Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);


                                            trazabilidades[i].DIASPROCESO = Math.Round((DateTime.Now - trazabilidades[i - 2].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i].DIASPROCESOESTADO = Math.Round((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                            break;
                                        case "Demostración Aprobada – Finiquitos":

                                            diasParaAjustar = OperadorDias(trazabilidades[i - 3].CMP_TRAZA_FECHA, trazabilidades[i - 1].CMP_TRAZA_FECHA);
                                            trazabilidades[i - 1].DIASPROCESOESTADO = Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i - 1].DIASPROCESO = DiasNegativos(Math.Ceiling((trazabilidades[i - 1].CMP_TRAZA_FECHA - trazabilidades[i - 3].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));

                                            trazabilidades[i].DIASPROCESO = Math.Round((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 3].CMP_TRAZA_FECHA).TotalDays);
                                            trazabilidades[i].DIASPROCESOESTADO = 0;
                                            break;
                                        case "Trámite Rechazado – Finiquitos":
                                            trazabilidades[i].DIASPROCESO = 0;
                                            trazabilidades[i].DIASPROCESOESTADO = 0;

                                            diasParaAjustar = OperadorDias(trazabilidades[i - 1].CMP_TRAZA_FECHA, trazabilidades[i].CMP_TRAZA_FECHA);
                                            trazabilidades[i - 1].DIASPROCESO = DiasNegativos(Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));
                                            trazabilidades[i - 1].DIASPROCESOESTADO = DiasNegativos(Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays - diasParaAjustar));

                                            break;
                                        case "Demostración Incompleta - Finiquitos":

                                            switch (trazabilidades[i - 1].CMP_TRAZA_ESTADO_DOC)
                                            {

                                                case "Con Documentos recibidos – Finiquitos":
                                                    trazabilidades[i - 1].DIASPROCESO = Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i - 1].DIASPROCESOESTADO = Math.Ceiling((trazabilidades[i].CMP_TRAZA_FECHA - trazabilidades[i - 1].CMP_TRAZA_FECHA).TotalDays);

                                                    trazabilidades[i].DIASPROCESO = Math.Ceiling((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);
                                                    trazabilidades[i].DIASPROCESOESTADO = Math.Floor((DateTime.Now - trazabilidades[i].CMP_TRAZA_FECHA).TotalDays);


                                                    break;
                                                default:
                                                    break;
                                            }


                                            trazabilidades[i].DIASPROCESO = 0;
                                            trazabilidades[i].DIASPROCESOESTADO = 0;
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                //Fin Funcionalidad

                                //if (contador == trazabilidades.Count)
                                //{

                                //    for (int dia = 0; dia < trazabilidades.Count; dia++)
                                //    {
                                //        if (trazabilidades[dia].CMP_TRAZA_ESTADO_DOC.Equals("Con Documentos recibidos – Finiquitos")
                                //            || trazabilidades[dia].CMP_TRAZA_ESTADO_DOC.Equals("En Revisión Recaudos y Pagos – Finiquitos")
                                //            || trazabilidades[dia].CMP_TRAZA_ESTADO_DOC.Equals("En Revisión Secretaría Técnica – Finiquitos"))
                                //        {

                                //            traza.DIAS_FEP = (int)(traza.DIAS_FEP + (trazabilidades[dia].DIASPROCESOESTADO == null ? 0 : trazabilidades[dia].DIASPROCESOESTADO));

                                //        }
                                //        else if (trazabilidades[dia].CMP_TRAZA_ESTADO_DOC.Equals("Pendiente Por Finiquito")
                                //            || trazabilidades[dia].CMP_TRAZA_ESTADO_DOC.Equals("Demostración Incompleta - Finiquitos")
                                //            || trazabilidades[dia].CMP_TRAZA_ESTADO_DOC.Equals("Trámite Rechazado – Finiquitos"))
                                //        {

                                //            traza.DIAS_EMIS_DCD = (int)(traza.DIAS_EMIS_DCD + (trazabilidades[dia].DIASPROCESOESTADO == null ? 0 : trazabilidades[dia].DIASPROCESOESTADO));

                                //        }
                                //    }

                                //}
                                //contador++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);

            }

            return data;
            
        }