import React from "react";
import { etiquetas } from "../api/clienteApi";
export default function Insignia({ valor }) {
  return <span className={`badge ${valor}`}>{etiquetas[valor] || valor}</span>;
}
