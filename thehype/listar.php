<?php
include("conexao.php");
include("menuproduto.php");

$sql = "select * from produto";
$query = $mysqli->query($sql);

?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
 "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1" />
<title>Lista de Dados</title>
</head>

<body>

<div id="tabela">
<table width="500" border="1" align="center">
  <tr>
    <th>Codigo</th>
    <th>Nome</th>
    <th>Preço</th>
    <th>Fornecedor</th>
    <th>Quantidade</th>
    <th>Opções</th>
  </tr>
  </div>
<?php
while($L = mysqli_fetch_array($query)) {
	$Codigo        = $L["Cod_produto"];
	$Nome      = $L["nome"];
  $preco     =$L["preco"];
	$Fornecedor = $L["fornecedor"];
	$Quantidade    = $L["quantidade"];
  
	
	echo"
  <tr>
    <td>$Codigo</td>
    <td>$Nome</td>
    <td>$preco</td>
    <td>$Fornecedor</td>
    <td>$Quantidade</td>
    
    <td><a href=\"editar.php?Cod_produto=$Codigo\">[Editar]</a> | 
	<a href=\"excluir.php?Cod_produto=$Codigo\">[Excluir]</a></td>
  </tr>\n";
}  
?> 
<?php
if(mysqli_fetch_array($query) >= 1) {
  $mysqli->close();
}
?> 
</table>

</body>
</html>

<style>

@import url(http://fonts.googleapis.com/css?family=Montserrat:400,700);

body {
	width: 1383px;
	max-width: 100%;
	overflow-x: hidden;
	height: 1200;
	font-family: Montserrat;
	margin: 0 auto;
}


h2 {
text-align: center;
}


#tabela table {
width: 100%;
border-collapse: collapse;
}


#tabela th {
background-color:#;
font-size: 12px;
color:#484848;
padding-left:4px;
padding-right:4px;
border-bottom:solid 1px #CCC;
height:26px;
padding-right:5px;

	}
#tabela tr:nth-child(odd) {
		background-color:#F3F3F3;
}

#tabela tr:nth-child(even) {
		background-color:#FFF;

}

#tabela tr, td {
height:26px;
padding-left:4px;
padding-right:2px;
white-space:nowrap;
border-bottom:solid 1px #E1E1E1;
}

#tabela tr:hover {
background-color: gray;
cursor:pointer; color:#FFF;
}

#logo {
position: relative;
left: 39%;
</style>