<?php

include("conexao.php");
$Cod_produto = $_GET["Cod_produto"];
settype($id, "integer");

$sql = "delete from produto where Cod_produto = $Cod_produto";
$query = $mysqli->query($sql);
$query->affected_rows;
		


$mysqli->close();
header("Location: listar.php");
?>
