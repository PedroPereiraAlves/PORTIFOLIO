<?php 
$Nome = $_POST['Nome'];
$Nascimento = $_POST['Nascimento'];
$CPF = $_POST['CPF'];
$Telefone = $_POST['numero_telefone']
$Email = $_POST['Email'];
$senha = MD5($_POST['senha']);
$connect = mysqli_connect('127.0.0.1','root','');
$db = mysqli_select_db('thehype');
$query_select = "SELECT cliente FROM login WHERE Email = '$Email'";
$select = mysqli_query($query_select,$connect);
$array = mysqli_fetch_array($select);
$logarray = $array['Email'];
 
  if($Email == "" || $Email == null){
    echo"<script language='javascript' type='text/javascript'>alert('O campo Email deve ser preenchido');window.location.href='login.html';</script>";
 
    }else{
      if($logarray == $Email){
 
        echo"<script language='javascript' type='text/javascript'>alert('Esse login já existe');window.location.href='login.html';</script>";
        die();
 
      }else{
        $query = "INSERT INTO cliente (Nome,CPF,Nacimento,numero_telefone,Email,senha) VALUES ('$Nome','$Nacimento','$CPF','$Telefone','$Email','$senha')";
        $insert = mysql_query($query,$connect);
         
        if($insert){
          echo"<script language='javascript' type='text/javascript'>alert('Usuário cadastrado com sucesso!');window.location.href='login.html'</script>";
        }else{
          echo"<script language='javascript' type='text/javascript'>alert('Não foi possível cadastrar esse usuário');window.location.href='login.html'</script>";
        }
      }
    }
?>