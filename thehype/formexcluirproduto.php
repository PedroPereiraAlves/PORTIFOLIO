<?php

// INCLUI A LISTA DE PRODUTOS CADASTRADOS

include "menuproduto.php";

?>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title></title>

</head>

<body>

<h1> EXCLUSÃO DE PRODUTOS </h1>

<?

// INCLUIR ARQUIVO DE CONEXÃO

include 'conexao.php';

// RECEBE DO MÉTODO GET O ID DO PRODUTO A SER EXCLUÍDO

$nome = $_GET["txtnome"];

$query = "SELECT * FROM produto WHERE nome = $nome";

$consulta = mysql_query($query);

if (mysql_num_rows($consulta) == 0){

echo "Não foi possivel Localizar nenhum produto com esse ID";

}

else

{

$row = mysql_fetch_array($consulta);

$nome = $row["nome"];

$preco = $row["preco"];

$fornecedor= $row["fornecedor"];

$quantidade = $row["quantidade"];
?>

<!-- EXIBIR OS DADOS DO PRODUTO-->

<b>Dados do PRODUTO</b> <br>

<b>Nome</b>: <? echo $nome; ?> <br>

<b>Preço</b>: <? echo $preco; ?> <br>

<b>Fornecedor</b>: <? echo $fornecedor; ?> <br>

<b>Quantidade</b>: <? echo $quantidade; ?> <br>

<br>

<hr width="50%" align="left">

<!-- FORMULÁRIO DE EXCLUSÃO DE PRODUTO -->

<form method="post" action="excluirproduto.php">

Tem certeza que deseja exluir o PRODUTO acima?<br />

<br>

<input type="radio" name="optresposta" value="N" checked />NÃO

<input type="radio" name="optresposta" value="S" />SIM

<!-- CAMPO OCULTO NO FORMULÁRIO COM O ID DO PRODUTO A SER EXCLUÍDO -->

<input type="hidden" name="txtnome" value="<?php echo $nome; ?>" />

<br><br>

<input type="submit" value="Excluir Produto" />

<input type="reset" value="Cancelar" />

</form>

<?php

 
// FIM DO COMANDO IF

// ENCERRANDO A CONEXÃO COM O BANCO

mysql_close();

?>

<br/>

<br/>

<a href='menuproduto.php'> Voltar Ao Menu </a>

</div>

</body>

</html>