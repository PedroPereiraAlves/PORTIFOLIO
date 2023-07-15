<?php

include("conexao.php");
$nome      	= $_POST["nome"];
$preco 		= $_POST["preco"];
$Fornecedor = $_POST["fornecedor"];
$Quantidade = $_POST["quantidade"]; 
$Codigo     = $_POST["Cod_produto"];

$sql = "UPDATE produto SET fornecedor = '$Fornecedor', quantidade = '$Quantidade', nome = '$nome',
preco = '$preco' 
 WHERE Cod_produto = $Codigo";
$query = $mysqli->query($sql);

$mysqli->close();
header("Location: listar.php");
?>

