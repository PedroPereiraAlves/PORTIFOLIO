<?php

// INCLUI A LISTA DE PRODUTO CADASTRADOS

include "menuproduto.php";

?>

<html>

<head>

<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">

<title>Controle de Produtos</title>

</head>

<body>

<?php

// INCLUINDO O ARQUIVO DE CONEXÃO COM O BANCO

include 'conexao.php';

//$campo = $_POST['cbocampo'];

$param = $_POST['txtparametro'];

// VERIFICA QUAL FOI O PARAMETRO DA BUSCA E GRAVA NA VARIAVEL QUERY A

// CONSULTA REFERENTE AO PARAMETRO.



if($param == 'Nome'){

$sql = "select * from produto where Nome like  '%".$param."%'";

}



else{

echo "O parametro informado é inválido";

}
//$query = $mysqli->query($sql);

$result = mysqli_num_rows($query);

// EXECUTA A CONSULTA NO BANCO
		

// VERIFICAR SE NENHUM REGISTRO FOI LOCALIZADO

if(mysqli_affected_rows() == 0){

echo "Nenhum registro foi Localizado";

}

// GRAVA A CONSULTA EM UM ARRAY PHP

while($row = mysqli_fetch_array($result))

{

// RECEBE OS DADOS DO PRODUTO CONSULTADO


$nome = $row['Nome'];

// EXIBE OS DADOS DOS ALUNOS LOCALIZADOS

echo "<br><br>";

echo "Registro(s) Localizado(s)...";

echo "<br><br>";

echo "<b>Nome</b>: ".$nome."<br>";



// CRIAR UM LINK PARA EXCLUIR OU ALTERAR PASSANDO VIA GET O ID

echo "<a href='formalterarproduto.php?txtid=$id'> Alterar </a> || <a href='formexcluirproduto.php?txtid=$id'> Excluir</a>";

echo "";
$mysqli->close();


}


?>

</body>


</html>