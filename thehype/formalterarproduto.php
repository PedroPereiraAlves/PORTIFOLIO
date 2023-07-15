<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<?php

// INCLUIR O ARQUIVO DE CONEXÃO COM O BANCO DE DADOS

include "conexao.php";

// RECEBENDO O id DO PRODUTO PASSADO PELO MÉTODO GET VIA URL

$id = $_GET['txtid'];

// VARIÁVEL COM O COMANDO DE CONSULTA NA TABELA PRODUTO

$query = "select * from produto where id = $id";

// COMANDO PARA EXECUTAR A CONSULTA NO BANCO

$result = mysql_query($query) or die(mysql_error());

if (mysql_num_rows($result) == 0)

echo "Não foi possivel Localizar nenhum produto com esse ID";

else

{

// GRAVAR A CONSULTA EXECUTA EM UM ARRAY CHAMADO $row

$row = mysql_fetch_array($result);

$nome = $row["nome"];

$preco= $row["preco"];

$fornecedor = $row["fornecedor"];

$quantidade = $row["quantidade"];
?>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title></title>

</head>

<body>

<H2>ALTERAÇÃO DE PRODUTOS</H2>

<form method="POST" action="alterarproduto.php">

<!-- CAMPO OCULTO COM NÚMERO DO id DO PRODUTO -->

<input type="hidden" name="txtid" value="<? echo $id; ?>"/>

<table width="auto" border="0">
<tr>
<td>id</td>	
<td><input type="text" name="txtid" id="txtid" value="<? echo $id; ?>"/></td>

</tr>

<tr>

<td>Nome</td>

<td><input type="text" name="txtnome" id="txtnome" size="45" value="<? echo $nome; ?>"/></td>

</tr>

<tr>

<td>Preço</td>

<td><input type="text" name="txtpreco" id="txtpreco" value="<? echo $preco; ?>"/></td>

</tr>

<tr>

<td>Quantidade</td>


<td><input type="text" name="txtquantidade" id="txtquantidade" value="<? echo $quantidade; ?>"/></td>


</tr>

<tr>

<td>Fornecedor</td>

<td><input type="text" name="txtfornecedor" id="txtfornecedor" value="<? echo $fornecedor; ?>"/></td>

</tr>


<td><input type="submit" name="button" id="button" value="Alterar" />

<input type="reset" name="button2" id="button2" value="Cancelar" /></td>

</tr>

</table>

</form>


</body>

</html>